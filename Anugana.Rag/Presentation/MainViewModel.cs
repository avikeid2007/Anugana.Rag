using CommunityToolkit.Mvvm.ComponentModel;

namespace Anugana.Rag.Presentation;

public partial class MainViewModel : ObservableObject
{
    public ChatViewModel ChatVm { get; }
    public IngestionViewModel IngestionVm { get; }
    public SettingsViewModel SettingsVm { get; }

    public MainViewModel(
        ChatViewModel chatVm,
        IngestionViewModel ingestionVm,
        SettingsViewModel settingsVm)
    {
        ChatVm = chatVm;
        IngestionVm = ingestionVm;
        SettingsVm = settingsVm;
    }
}
