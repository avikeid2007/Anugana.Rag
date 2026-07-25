using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public class RestVectorDbService : IVectorDbService
{
    private readonly ISettingsService _settingsService;
    private readonly HttpClient _httpClient;

    public RestVectorDbService(ISettingsService settingsService, HttpClient httpClient)
    {
        _settingsService = settingsService;
        _httpClient = httpClient;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _settingsService.CurrentSettings;
            var response = await _httpClient.GetAsync($"{settings.RestVectorDbEndpoint.TrimEnd('/')}/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task EnsureCollectionAsync(string collectionName, ulong vectorSize, CancellationToken cancellationToken = default)
    {
        // Custom REST implementation stub / collection initialization
        return Task.CompletedTask;
    }

    public async Task UpsertChunksAsync(string collectionName, IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.CurrentSettings;
        var payload = JsonSerializer.Serialize(new { collection = collectionName, chunks });
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        await _httpClient.PostAsync($"{settings.RestVectorDbEndpoint.TrimEnd('/')}/upsert", content, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<VectorSearchResult>> SearchSimilarAsync(
        string collectionName,
        float[] queryVector,
        int topK,
        float scoreThreshold,
        CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.CurrentSettings;
        var req = JsonSerializer.Serialize(new { collection = collectionName, vector = queryVector, topK, scoreThreshold });
        var content = new StringContent(req, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{settings.RestVectorDbEndpoint.TrimEnd('/')}/search", content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return new List<VectorSearchResult>();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var list = JsonSerializer.Deserialize<List<VectorSearchResult>>(json);
        return list ?? new List<VectorSearchResult>();
    }

    public async Task<(ulong PointsCount, string Status)> GetStatsAsync(string collectionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _settingsService.CurrentSettings;
            var response = await _httpClient.GetAsync($"{settings.RestVectorDbEndpoint.TrimEnd('/')}/stats?collection={collectionName}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return (0, "Offline");

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var count = doc.RootElement.GetProperty("count").GetUInt64();
            var status = doc.RootElement.GetProperty("status").GetString() ?? "OK";
            return (count, status);
        }
        catch
        {
            return (0, "Error");
        }
    }
}
