using CommunityToolkit.Mvvm.ComponentModel;

namespace Anugana.Rag.Presentation;

public partial class AboutViewModel : ObservableObject
{
    public string AppName => "Anugana AI - RAG Assistant";
    public string AppVersion => "Version 1.0.0";
    public string Publisher => "Avnish (Anugana Systems)";
    public string TechnologyStack => "Uno Platform, .NET 10, Qdrant Vector DB, OpenRouter AI, SQLite";
    public string Description => "Anugana AI RAG is an intelligent, privacy-focused cross-platform document assistant powered by vector embeddings and generative AI.";

    public AboutViewModel()
    {
    }
}
