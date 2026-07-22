using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public interface IVectorDbService
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task EnsureCollectionAsync(string collectionName, ulong vectorSize, CancellationToken cancellationToken = default);
    Task UpsertChunksAsync(string collectionName, IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<List<VectorSearchResult>> SearchSimilarAsync(string collectionName, float[] queryVector, int topK, float scoreThreshold, CancellationToken cancellationToken = default);
    Task<(ulong PointsCount, string Status)> GetStatsAsync(string collectionName, CancellationToken cancellationToken = default);
}
