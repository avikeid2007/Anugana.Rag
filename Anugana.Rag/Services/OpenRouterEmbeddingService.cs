using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public class OpenRouterEmbeddingService : IEmbeddingService
{
    private readonly ISettingsService _settingsService;
    private readonly HttpClient _httpClient;

    public OpenRouterEmbeddingService(ISettingsService settingsService, HttpClient httpClient)
    {
        _settingsService = settingsService;
        _httpClient = httpClient;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var batch = await GetEmbeddingsBatchAsync(new List<string> { text }, cancellationToken);
            return batch.Count > 0 ? batch[0] : Array.Empty<float>();
        }
        catch
        {
            return Array.Empty<float>();
        }
    }

    public async Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0) return new List<float[]>();

        try
        {
            var settings = _settingsService.CurrentSettings;
            var baseUrl = settings.OpenRouterBaseUrl.TrimEnd('/');
            var url = $"{baseUrl}/embeddings";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(settings.OpenRouterApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenRouterApiKey);
            }
            request.Headers.Add("HTTP-Referer", "https://anugana.rag");
            request.Headers.Add("X-Title", "Anugana.Rag");

            var payload = JsonSerializer.Serialize(new
            {
                model = string.IsNullOrWhiteSpace(settings.EmbeddingModel) ? "openai/text-embedding-3-small" : settings.EmbeddingModel,
                input = texts
            });

            request.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new List<float[]>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            var result = new List<float[]>();
            if (doc.RootElement.TryGetProperty("data", out var dataArray))
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (item.TryGetProperty("embedding", out var embElement))
                    {
                        var floats = new float[embElement.GetArrayLength()];
                        int idx = 0;
                        foreach (var val in embElement.EnumerateArray())
                        {
                            floats[idx++] = val.GetSingle();
                        }
                        result.Add(floats);
                    }
                }
            }

            return result;
        }
        catch
        {
            return new List<float[]>();
        }
    }
}
