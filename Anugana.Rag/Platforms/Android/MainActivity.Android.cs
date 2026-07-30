using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Anugana.Rag.Droid;

[Activity(
    Label = "Anugana AI - Rag Assistant",
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);
        global::AndroidX.Activity.EdgeToEdge.Enable(this);

        base.OnCreate(savedInstanceState);

        if (Window != null)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
            }

            if (Build.VERSION.SdkInt < (BuildVersionCodes)35) // API < 35 (Android 15)
            {
#pragma warning disable CS0618
                Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
#pragma warning restore CS0618
            }
        }
    }

}
