using System.Collections.ObjectModel;
using System.Linq;

using Anugana.Rag.Services;

namespace Anugana.Rag.Presentation;

public partial class ChatViewModel : ObservableObject
{
    private readonly IRagPipelineService _ragPipelineService;

    [ObservableProperty]
    private string _userInputText = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ChatViewModel(IRagPipelineService ragPipelineService)
    {
        _ragPipelineService = ragPipelineService;

        // Welcome message
        Messages.Add(new ChatMessage
        {
            Sender = "Assistant",
            Content = "Hello! I am your **Anugana.Rag** Assistant. Upload your PDFs or text documents in the Knowledge Base, then ask me anything!",
            Timestamp = DateTimeOffset.Now
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInputText) || IsProcessing) return;

        var userMessageText = UserInputText.Trim();
        UserInputText = string.Empty;

        var userMsg = new ChatMessage
        {
            Sender = "User",
            Content = userMessageText,
            Timestamp = DateTimeOffset.Now
        };
        Messages.Add(userMsg);

        // Capture clean history BEFORE adding the streaming placeholder to avoid
        // passing the "..." assistant stub into the RAG pipeline / LLM context
        var chatHistory = Messages.Where(m => !m.IsStreaming).ToList();

        var assistantMsg = new ChatMessage
        {
            Sender = "Assistant",
            Content = string.Empty,
            Timestamp = DateTimeOffset.Now,
            IsStreaming = true,
            IsThinking = true
        };
        Messages.Add(assistantMsg);

        IsProcessing = true;

        try
        {
            var (citations, stream) = await _ragPipelineService.ProcessQueryAsync(chatHistory, CancellationToken.None);
            assistantMsg.Citations = citations;

            await foreach (var token in stream.WithCancellation(CancellationToken.None))
            {
                if (assistantMsg.IsThinking)
                {
                    assistantMsg.IsThinking = false;
                }
                assistantMsg.Content += token;
            }
        }
        catch (Exception ex)
        {
            assistantMsg.IsThinking = false;
            assistantMsg.Content = $"⚠️ Error generating response: {ex.Message}";
        }
        finally
        {
            assistantMsg.IsThinking = false;
            assistantMsg.IsStreaming = false;
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void ClearMessages()
    {
        Messages.Clear();
        Messages.Add(new ChatMessage
        {
            Sender = "Assistant",
            Content = "Chat history cleared. How can I assist you with your knowledge base?",
            Timestamp = DateTimeOffset.Now
        });
    }
}
