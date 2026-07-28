using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Anugana.Rag.Presentation;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
    }

    private async void OpenAboutDialog_Click(object sender, RoutedEventArgs e)
    {
        if (AboutDialog != null)
        {
            AboutDialog.XamlRoot = this.XamlRoot;
            await AboutDialog.ShowAsync();
        }
    }
}
