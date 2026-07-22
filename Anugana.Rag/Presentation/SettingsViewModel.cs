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
        _settingsService.SaveSettings(Settings);
        ThemeHelper.ApplyTheme(Settings.Theme);
    }

    [RelayCommand]
    private async Task TestVectorDbAsync(CancellationToken cancellationToken)
    {
        DbConnectionStatus = "Testing connection...";
        bool ok = await _vectorDbService.TestConnectionAsync(cancellationToken);
        DbConnectionStatus = ok ? "✅ Connection Successful!" : "❌ Connection Failed.";
    }

    [RelayCommand]
    private async Task TestLlmAsync(CancellationToken cancellationToken)
    {
        LlmConnectionStatus = "Testing connection...";
        bool ok = await _llmService.TestConnectionAsync(cancellationToken);
        LlmConnectionStatus = ok ? "✅ Connection Successful!" : "❌ Connection Failed.";
    }
}
