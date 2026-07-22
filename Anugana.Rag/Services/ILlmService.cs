using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public interface ILlmService
{
    IAsyncEnumerable<string> StreamCompletionAsync(
        List<ChatMessage> history,
        string systemPrompt,
        CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
