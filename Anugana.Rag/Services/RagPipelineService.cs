using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public interface IRagPipelineService
{
    Task<(List<VectorSearchResult> Citations, IAsyncEnumerable<string> Stream)> ProcessQueryAsync(
        List<ChatMessage> history,
        CancellationToken cancellationToken = default);
}

public class RagPipelineService : IRagPipelineService
{
    private readonly ISettingsService _settingsService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorDbService _vectorDbService;
    private readonly ILlmService _llmService;

    public RagPipelineService(
        ISettingsService settingsService,
        IEmbeddingService embeddingService,
        IVectorDbService vectorDbService,
        ILlmService llmService)
    {
        _settingsService = settingsService;
        _embeddingService = embeddingService;
        _vectorDbService = vectorDbService;
        _llmService = llmService;
    }

    public async Task<(List<VectorSearchResult> Citations, IAsyncEnumerable<string> Stream)> ProcessQueryAsync(
        List<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var settings = _settingsService.CurrentSettings;
        var lastUserMsg = history.LastOrDefault(m => m.Sender.Equals("User", System.StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;

        var citations = new List<VectorSearchResult>();

        if (!string.IsNullOrWhiteSpace(lastUserMsg))
        {
            try
            {
                var queryVector = await _embeddingService.GetEmbeddingAsync(lastUserMsg, cancellationToken);
                if (queryVector.Length > 0)
                {
                    float effectiveThreshold = settings.ScoreThreshold <= 0.001f ? 0.30f : settings.ScoreThreshold;
                    int effectiveTopK = settings.TopK <= 0 ? 4 : settings.TopK;

                    citations = await _vectorDbService.SearchSimilarAsync(
                        settings.CollectionName,
                        queryVector,
                        effectiveTopK,
                        effectiveThreshold,
                        cancellationToken);
                }
            }
            catch
            {
                // Proceed with general knowledge if vector search encounters issue
            }
        }

        var augmentedSystemPrompt = new StringBuilder();
        augmentedSystemPrompt.AppendLine(settings.SystemPrompt);
        augmentedSystemPrompt.AppendLine();

        if (citations.Count > 0)
        {
            augmentedSystemPrompt.AppendLine("=== RETRIEVED CONTEXT SNIPPETS ===");
            for (int i = 0; i < citations.Count; i++)
            {
                var c = citations[i];
                augmentedSystemPrompt.AppendLine($"[Source {i + 1}: {c.FileName} (Page {c.PageIndex})] (Relevance: {c.Score:P0})");
                augmentedSystemPrompt.AppendLine(c.TextContent);
                augmentedSystemPrompt.AppendLine("-----------------------------------");
            }
            augmentedSystemPrompt.AppendLine("Instructions: Use the above context snippets to answer the user's request. Cite sources if applicable.");
        }
        else
        {
            augmentedSystemPrompt.AppendLine("Note: No specific context snippets were found in the knowledge base for this query.");
        }

        // Only include actual user/assistant dialogue turns in LLM context (exclude system/welcome messages)
        var llmHistory = history
            .Where(m => m.Sender.Equals("User", StringComparison.OrdinalIgnoreCase)
                     || m.Sender.Equals("Assistant", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var stream = _llmService.StreamCompletionAsync(llmHistory, augmentedSystemPrompt.ToString(), cancellationToken);
        return (citations, stream);
    }
}
