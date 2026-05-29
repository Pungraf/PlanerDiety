using Android.App;
using Android.Content.PM;
using Android.Content;
using DietPlanner.Mobile.Services;
using Microsoft.Maui;

namespace DietPlanner.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public sealed class MainActivity : MauiAppCompatActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (!GoogleSignInActivityResultBroker.TryHandleResult(requestCode, resultCode, data))
        {
            base.OnActivityResult(requestCode, resultCode, data);
            return;
        }

        base.OnActivityResult(requestCode, resultCode, data);
    }
}
