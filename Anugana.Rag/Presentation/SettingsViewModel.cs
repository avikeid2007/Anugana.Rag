using System;
using System.Threading;
using System.Threading.Tasks;
using Anugana.Rag.Models;
using Anugana.Rag.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Anugana.Rag.Presentation;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IVectorDbService _vectorDbService;
    private readonly ILlmService _llmService;

    [ObservableProperty]
    private AppSettings _settings;

    [ObservableProperty]
    private string _dbConnectionStatus = "Not Tested";

    [ObservableProperty]
    private string _llmConnectionStatus = "Not Tested";

    public SettingsViewModel(
        ISettingsService settingsService,
        IVectorDbService vectorDbService,
        ILlmService llmService)
    {
        _settingsService = settingsService;
        _vectorDbService = vectorDbService;
        _llmService = llmService;

        _settings = _settingsService.CurrentSettings;
        ThemeHelper.ApplyTheme(_settings.Theme);
    }

    [RelayCommand]
    public void SetTheme(string themeName)
    {
        if (Enum.TryParse<RagAppTheme>(themeName, out var theme))
        {
            Settings.Theme = theme;
            ThemeHelper.ApplyTheme(theme);
            SaveSettings();
        }
    }

    [RelayCommand]
    private void SaveSettings()
    {
        if (Settings != null)
        {
            Settings.QdrantApiKey = Settings.QdrantApiKey?.Trim() ?? string.Empty;
            Settings.OpenRouterApiKey = Settings.OpenRouterApiKey?.Trim() ?? string.Empty;
            Settings.QdrantEndpoint = Settings.QdrantEndpoint?.Trim() ?? string.Empty;
            Settings.OpenRouterBaseUrl = Settings.OpenRouterBaseUrl?.Trim() ?? string.Empty;
        }
        _settingsService.SaveSettings(Settings);
        ThemeHelper.ApplyTheme(Settings.Theme);
    }

    [RelayCommand]
    private async Task TestVectorDbAsync(CancellationToken cancellationToken)
    {
        DbConnectionStatus = "Testing connection...";
        bool ok = await Task.Run(async () => await _vectorDbService.TestConnectionAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        DbConnectionStatus = ok ? "✅ Connection Successful!" : "❌ Connection Failed.";
    }

    [RelayCommand]
    private async Task TestLlmAsync(CancellationToken cancellationToken)
    {
        LlmConnectionStatus = "Testing connection...";
        bool ok = await Task.Run(async () => await _llmService.TestConnectionAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        LlmConnectionStatus = ok ? "✅ Connection Successful!" : "❌ Connection Failed.";
    }

    [RelayCommand]
    private async Task PasteQdrantEndpointAsync()
    {
        var text = await GetClipboardTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Settings.QdrantEndpoint = text.Trim();
            OnPropertyChanged(nameof(Settings));
        }
    }

    [RelayCommand]
    private async Task PasteQdrantApiKeyAsync()
    {
        var text = await GetClipboardTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Settings.QdrantApiKey = text.Trim();
            OnPropertyChanged(nameof(Settings));
        }
    }

    [RelayCommand]
    private async Task PasteOpenRouterApiKeyAsync()
    {
        var text = await GetClipboardTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Settings.OpenRouterApiKey = text.Trim();
            OnPropertyChanged(nameof(Settings));
        }
    }

    [RelayCommand]
    private async Task PasteOpenRouterBaseUrlAsync()
    {
        var text = await GetClipboardTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            Settings.OpenRouterBaseUrl = text.Trim();
            OnPropertyChanged(nameof(Settings));
        }
    }

    private static async Task<string?> GetClipboardTextAsync()
    {
        try
        {
            var content = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (content != null && content.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                return await content.GetTextAsync();
            }
        }
        catch
        {
            // Clipboard unavailable or empty
        }
        return null;
    }
}
