using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anugana.Rag.Services;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<List<float[]>> GetEmbeddingsBatchAsync(List<string> texts, CancellationToken cancellationToken = default);
}
