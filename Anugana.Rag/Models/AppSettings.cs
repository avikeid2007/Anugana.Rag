using SQLite;

namespace Anugana.Rag.Models;

public enum VectorDbProviderType
{
    Qdrant,
    CustomRestApi
}

public enum AiProviderType
{
    OpenRouter,
    OllamaLocal,
    CustomRest
}

public enum RagAppTheme
{
    System,
    Light,
    Dark
}

[Table("AppSettings")]
public class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public RagAppTheme Theme { get; set; } = RagAppTheme.System;

    public VectorDbProviderType VectorDbProvider { get; set; } = VectorDbProviderType.Qdrant;
    public string QdrantEndpoint { get; set; } = "http://localhost:6334";
    public string QdrantApiKey { get; set; } = string.Empty;

    public string RestVectorDbEndpoint { get; set; } = "http://localhost:8080";
    public string RestVectorDbApiKey { get; set; } = string.Empty;

    public AiProviderType AiProvider { get; set; } = AiProviderType.OpenRouter;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string OpenRouterBaseUrl { get; set; } = "https://openrouter.ai/api/v1";

    public string ChatModel { get; set; } = "meta-llama/llama-3.3-70b-instruct";
    public string EmbeddingModel { get; set; } = "openai/text-embedding-3-small";
    public string CollectionName { get; set; } = "documents";

    private int _chunkSize = 500;
    public int ChunkSize
    {
        get => _chunkSize <= 0 ? 500 : _chunkSize;
        set => _chunkSize = value <= 0 ? 500 : value;
    }

    private int _chunkOverlap = 50;
    public int ChunkOverlap
    {
        get => _chunkOverlap < 0 ? 50 : _chunkOverlap;
        set => _chunkOverlap = value < 0 ? 50 : value;
    }

    private int _topK = 4;
    public int TopK
    {
        get => _topK <= 0 ? 4 : _topK;
        set => _topK = value <= 0 ? 4 : value;
    }

    private float _scoreThreshold = 0.50f;
    public float ScoreThreshold
    {
        get => _scoreThreshold <= 0.001f ? 0.50f : _scoreThreshold;
        set => _scoreThreshold = value <= 0.001f ? 0.50f : value;
    }

    public string SystemPrompt { get; set; } =
        "You are a helpful RAG AI Assistant. Answer questions accurately using ONLY the provided context snippets below. If the context does not contain the answer, state that clearly and briefly.";
}
