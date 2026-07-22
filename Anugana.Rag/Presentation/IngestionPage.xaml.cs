using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Anugana.Rag.Presentation;

public sealed partial class IngestionPage : Page
{
    public IngestionPage()
    {
        this.InitializeComponent();
    }

    private async void SelectFilesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            try
            {
                var window = (App.Current as App)?.MainWindow;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }
            }
            catch { }

            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add(".txt");
            picker.FileTypeFilter.Add(".md");
            picker.FileTypeFilter.Add("*");

            var pickedFiles = await picker.PickMultipleFilesAsync();
            if (pickedFiles != null && pickedFiles.Count > 0)
            {
                var paths = pickedFiles.Select(f => f.Path).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                if (paths.Count > 0 && DataContext is IngestionViewModel vm)
                {
                    await vm.IngestMultipleFilesAsync(paths);
                }
            }
        }
        catch (Exception ex)
        {
            if (DataContext is IngestionViewModel vm)
            {
                vm.StatusMessage = $"⚠️ Error opening file picker: {ex.Message}";
            }
        }
    }
}
