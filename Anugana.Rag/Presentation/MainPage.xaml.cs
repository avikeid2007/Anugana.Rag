using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Toolkit.UI;

namespace Anugana.Rag.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }

    private void BottomTabBar_SelectionChanged(object sender, TabBarSelectionChangedEventArgs e)
    {
        if (BottomTabBar == null || ChatPageContainer == null) return;

        int index = BottomTabBar.SelectedIndex;
        ChatPageContainer.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        IngestionPageContainer.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        SettingsPageContainer.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
    }
}
