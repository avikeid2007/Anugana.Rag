using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Anugana.Rag.Models;

public enum DocumentStatus
{
    Pending,
    Parsing,
    Embedding,
    Indexing,
    Completed,
    Failed
}

public class IngestedDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public int ChunkCount { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.Now;

    public string FormattedChunkCount => $"{ChunkCount} chunks";
}

public class DocumentChunk
{
    public string ChunkId { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int PageIndex { get; set; } = 1;
    public int ChunkIndex { get; set; }
    public string TextContent { get; set; } = string.Empty;
    public float[]? Vector { get; set; }
}

public class VectorSearchResult
{
    public string ChunkId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int PageIndex { get; set; } = 1;
    public string TextContent { get; set; } = string.Empty;
    public float Score { get; set; }

    public string FormattedScore => $"{Score:P0}";
}

public partial class ChatMessage : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Sender { get; set; } = "User"; // "User", "Assistant", "System"

    [ObservableProperty]
    private string _content = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _isThinking;

    [ObservableProperty]
    private bool _isCopied;

    public string CopyButtonText => IsCopied ? "✅ Copied!" : "📋 Copy";

    partial void OnIsCopiedChanged(bool value)
    {
        OnPropertyChanged(nameof(CopyButtonText));
    }

    [ObservableProperty]
    private List<VectorSearchResult> _citations = new();

    public string FormattedTimestamp => Timestamp.ToString("HH:mm");

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task CopyAsync()
    {
        if (string.IsNullOrWhiteSpace(Content)) return;

        try
        {
            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.SetText(Content);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);

            IsCopied = true;
            await Task.Delay(2000);
            IsCopied = false;
        }
        catch { }
    }
}
