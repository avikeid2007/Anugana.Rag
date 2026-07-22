using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Anugana.Rag.Services;

/// <summary>
/// Qdrant Vector DB service using the same payload schema as Anugana.Shared:
///   text        = chunk text content
///   file_name   = source file name
///   page_number = page index (1-based)
///   chunk_index = chunk ordinal within document
/// </summary>
public class QdrantVectorDbService : IVectorDbService
{
    private readonly ISettingsService _settingsService;
    private readonly HttpClient _httpClient;

    static QdrantVectorDbService()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    public QdrantVectorDbService(ISettingsService settingsService, HttpClient httpClient)
    {
        _settingsService = settingsService;
        _httpClient = httpClient;
    }

    // ─── gRPC Client Factory (matches QdrantClientWrapper.cs pattern) ────────────

    private QdrantClient GetGrpcClient()
    {
        var settings = _settingsService.CurrentSettings;
        var url = (settings.QdrantEndpoint ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(url)) url = "http://localhost:6334";

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var scheme = !string.IsNullOrWhiteSpace(settings.QdrantApiKey) ? "https" : "http";
            url = $"{scheme}://{url}";
        }

        var apiKey = string.IsNullOrWhiteSpace(settings.QdrantApiKey) ? null : settings.QdrantApiKey;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            bool isDefaultHttps = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                                  (uri.Port == 443 || uri.IsDefaultPort);
            if (isDefaultHttps)
                return new QdrantClient(uri.Host, port: 6334, https: true, apiKey: apiKey);

            return new QdrantClient(uri, apiKey: apiKey);
        }

        return new QdrantClient(url, apiKey: apiKey);
    }

    // ─── REST Base URL ────────────────────────────────────────────────────────────

    private string GetRestBaseUrl()
    {
        var settings = _settingsService.CurrentSettings;
        var url = (settings.QdrantEndpoint ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(url)) url = "http://localhost:6333";

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var scheme = !string.IsNullOrWhiteSpace(settings.QdrantApiKey) ? "https" : "http";
            url = $"{scheme}://{url}";
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            // gRPC port 6334 → remap to REST port 6333 for HTTP calls
            int port = uri.Port == 6334 ? 6333 : uri.Port;
            return new UriBuilder(uri.Scheme, uri.Host, port).Uri.ToString().TrimEnd('/');
        }

        return url.TrimEnd('/');
    }

    private void AddApiKeyHeader(HttpRequestMessage req)
    {
        var apiKey = _settingsService.CurrentSettings.QdrantApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey)) req.Headers.Add("api-key", apiKey);
    }

    // ─── Public Interface ─────────────────────────────────────────────────────────

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = GetGrpcClient();
            var collections = await client.ListCollectionsAsync(cancellationToken);
            return collections != null;
        }
        catch { }

        // REST fallback
        try
        {
            var url = GetRestBaseUrl() + "/collections";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            AddApiKeyHeader(req);
            var resp = await _httpClient.SendAsync(req, cancellationToken);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnsureCollectionAsync(string collectionName, ulong vectorSize, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = GetGrpcClient();
            var existing = await client.ListCollectionsAsync(cancellationToken);
            if (!existing.Contains(collectionName))
            {
                await client.CreateCollectionAsync(
                    collectionName: collectionName,
                    vectorsConfig: new VectorParams { Size = vectorSize, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // REST fallback
            try
            {
                var checkUrl = GetRestBaseUrl() + $"/collections/{collectionName}";
                using var checkReq = new HttpRequestMessage(HttpMethod.Get, checkUrl);
                AddApiKeyHeader(checkReq);
                var checkResp = await _httpClient.SendAsync(checkReq, cancellationToken);

                if (!checkResp.IsSuccessStatusCode)
                {
                    var body = JsonSerializer.Serialize(new { vectors = new { size = vectorSize, distance = "Cosine" } });
                    using var createReq = new HttpRequestMessage(HttpMethod.Put, checkUrl)
                    {
                        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                    };
                    AddApiKeyHeader(createReq);
                    await _httpClient.SendAsync(createReq, cancellationToken);
                }
            }
            catch { }
        }
    }

    public async Task UpsertChunksAsync(string collectionName, IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.Where(c => c.Vector != null).ToList();
        if (!chunkList.Any()) return;

        ulong firstSize = (ulong)chunkList[0].Vector!.Length;
        await EnsureCollectionAsync(collectionName, firstSize, cancellationToken);

        try
        {
            using var client = GetGrpcClient();
            var points = new List<PointStruct>();

            foreach (var chunk in chunkList)
            {
                // Use ChunkId as Guid if parseable (matches QdrantClientWrapper behaviour)
                var id = Guid.TryParse(chunk.ChunkId, out var parsed) ? parsed : Guid.NewGuid();

                var point = new PointStruct { Id = id, Vectors = chunk.Vector! };

                // Use SAME payload keys as Anugana.Shared QdrantClientWrapper:
                point.Payload.Add("text", chunk.TextContent);
                point.Payload.Add("file_name", chunk.FileName);
                point.Payload.Add("page_number", chunk.PageIndex);
                point.Payload.Add("chunk_index", chunk.ChunkIndex);

                points.Add(point);
            }

            await client.UpsertAsync(collectionName, points, cancellationToken: cancellationToken);
        }
        catch
        {
            // REST fallback
            await UpsertRestAsync(collectionName, chunkList, cancellationToken);
        }
    }

    private async Task UpsertRestAsync(string collectionName, List<DocumentChunk> chunkList, CancellationToken cancellationToken)
    {
        try
        {
            var url = GetRestBaseUrl() + $"/collections/{collectionName}/points?wait=true";
            var pointsPayload = chunkList.Select(c =>
            {
                var id = Guid.TryParse(c.ChunkId, out var parsed) ? parsed.ToString() : Guid.NewGuid().ToString();
                return new
                {
                    id,
                    vector = c.Vector,
                    payload = new
                    {
                        text = c.TextContent,
                        file_name = c.FileName,
                        page_number = c.PageIndex,
                        chunk_index = c.ChunkIndex
                    }
                };
            });

            var json = JsonSerializer.Serialize(new { points = pointsPayload });
            using var req = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
            AddApiKeyHeader(req);
            await _httpClient.SendAsync(req, cancellationToken);
        }
        catch { }
    }

    // ─── Search: REST with with_payload=true for reliable payload retrieval ───────

    public async Task<List<VectorSearchResult>> SearchSimilarAsync(
        string collectionName,
        float[] queryVector,
        int topK,
        float scoreThreshold,
        CancellationToken cancellationToken = default)
    {
        var results = await SearchRestAsync(collectionName, queryVector, topK, scoreThreshold, cancellationToken);

        // Retry with 0.0 threshold if nothing matched
        if (results.Count == 0 && scoreThreshold > 0.0f)
        {
            results = await SearchRestAsync(collectionName, queryVector, topK, 0.0f, cancellationToken);
        }

        return results;
    }

    private async Task<List<VectorSearchResult>> SearchRestAsync(
        string collectionName,
        float[] queryVector,
        int topK,
        float scoreThreshold,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = GetRestBaseUrl() + $"/collections/{collectionName}/points/search";

            var payload = JsonSerializer.Serialize(new
            {
                vector = queryVector,
                limit = topK,
                score_threshold = scoreThreshold,
                with_payload = true,
                with_vector = false
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            };
            AddApiKeyHeader(req);

            var resp = await _httpClient.SendAsync(req, cancellationToken);
            if (!resp.IsSuccessStatusCode) return new List<VectorSearchResult>();

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var results = new List<VectorSearchResult>();

            if (doc.RootElement.TryGetProperty("result", out var resultArr))
            {
                foreach (var item in resultArr.EnumerateArray())
                {
                    float score = item.TryGetProperty("score", out var scoreEl) ? scoreEl.GetSingle() : 0f;

                    string chunkId = "", fileName = "", textContent = "";
                    int pageIndex = 1;

                    if (item.TryGetProperty("payload", out var p))
                    {
                        // Primary keys (Anugana.Shared schema)
                        textContent = GetStr(p, "text");
                        fileName = GetStr(p, "file_name");
                        pageIndex = GetInt(p, "page_number");
                        chunkId = GetStr(p, "id");

                        // Fallback to this app's own schema if primary keys are missing
                        if (string.IsNullOrEmpty(textContent)) textContent = GetStr(p, "textContent", "text_content", "content", "page_content");
                        if (string.IsNullOrEmpty(fileName)) fileName = GetStr(p, "fileName", "source", "document_name");
                        if (pageIndex == 1 && !p.TryGetProperty("page_number", out _)) pageIndex = GetInt(p, "pageIndex", "page_index");
                        if (string.IsNullOrEmpty(chunkId)) chunkId = GetStr(p, "chunkId", "chunk_id");
                    }

                    results.Add(new VectorSearchResult
                    {
                        Score = score,
                        ChunkId = chunkId,
                        FileName = fileName,
                        PageIndex = pageIndex,
                        TextContent = textContent
                    });
                }
            }

            return results;
        }
        catch
        {
            return new List<VectorSearchResult>();
        }
    }

    // ─── Payload helpers ──────────────────────────────────────────────────────────

    private static string GetStr(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (el.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null)
            {
                var s = v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "") : v.ToString();
                if (!string.IsNullOrEmpty(s)) return s;
            }
        }
        return string.Empty;
    }

    private static int GetInt(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (el.TryGetProperty(key, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n)) return n;
                if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out int ns)) return ns;
            }
        }
        return 1;
    }

    // ─── Stats ────────────────────────────────────────────────────────────────────

    public async Task<(ulong PointsCount, string Status)> GetStatsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = GetRestBaseUrl() + $"/collections/{collectionName}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            AddApiKeyHeader(req);
            var resp = await _httpClient.SendAsync(req, cancellationToken);

            if (!resp.IsSuccessStatusCode) return (0, "Not Found");

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("result", out var result))
            {
                ulong count = result.TryGetProperty("points_count", out var pc) ? pc.GetUInt64() : 0;
                string status = result.TryGetProperty("status", out var st) ? (st.GetString() ?? "OK") : "OK";
                return (count, status);
            }
        }
        catch { }

        return (0, "Offline");
    }
}
