using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public class OpenRouterLlmService : ILlmService
{
    private readonly ISettingsService _settingsService;
    private readonly HttpClient _httpClient;

    public OpenRouterLlmService(ISettingsService settingsService, HttpClient httpClient)
    {
        _settingsService = settingsService;
        _httpClient = httpClient;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = _settingsService.CurrentSettings;
            var url = $"{settings.OpenRouterBaseUrl.TrimEnd('/')}/models";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(settings.OpenRouterApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenRouterApiKey);
            }
            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        List<ChatMessage> history,
        string systemPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.CurrentSettings;
        var url = $"{settings.OpenRouterBaseUrl.TrimEnd('/')}/chat/completions";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (!string.IsNullOrWhiteSpace(settings.OpenRouterApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenRouterApiKey);
        }
        request.Headers.Add("HTTP-Referer", "https://anugana.rag");
        request.Headers.Add("X-Title", "Anugana.Rag");

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var msg in history)
        {
            messages.Add(new
            {
                role = msg.Sender.ToLowerInvariant() == "user" ? "user" : "assistant",
                content = msg.Content
            });
        }

        var payload = JsonSerializer.Serialize(new
        {
            model = string.IsNullOrWhiteSpace(settings.ChatModel) ? "meta-llama/llama-3.3-70b-instruct" : settings.ChatModel,
            messages,
            stream = true
        });

        request.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        string? networkErrorMsg = null;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            networkErrorMsg = $"⚠️ HTTP Network Error: {ex.Message}. Please check internet connection & Base URL in Settings.";
        }

        if (networkErrorMsg != null)
        {
            yield return networkErrorMsg;
            yield break;
        }

        if (response != null && !response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return $"⚠️ OpenRouter API Error ({(int)response.StatusCode} {response.ReasonPhrase}): {errContent}\n\nPlease check your API Key and Chat Model in Settings.";
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;

                string? contentToken = null;
                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentElement))
                        {
                            contentToken = contentElement.GetString();
                        }
                    }
                }
                catch
                {
                    // Ignore heartbeat/parse tokens
                }

                if (!string.IsNullOrEmpty(contentToken))
                {
                    yield return contentToken;
                }
            }
        }
    }
}
