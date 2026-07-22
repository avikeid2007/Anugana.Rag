using Anugana.Rag.Models;
using Microsoft.UI.Xaml;

namespace Anugana.Rag.Presentation;

public static class ThemeHelper
{
    public static void ApplyTheme(RagAppTheme theme)
    {
        try
        {
            var elementTheme = theme switch
            {
                RagAppTheme.Light => ElementTheme.Light,
                RagAppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (App.Current is App app && app.MainWindow?.Content is FrameworkElement root)
            {
                root.RequestedTheme = elementTheme;
            }
        }
        catch { }
    }
}
