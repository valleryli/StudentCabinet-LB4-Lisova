using Android.App;
using Android.Content.PM;
using Android.OS;
using StudentCabinet_LB1.Services;

namespace StudentCabinet_LB1
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                               ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(
        new[] { Android.Content.Intent.ActionView },
        Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
        DataScheme = "studentcabinet",
        DataHost = "student")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleIntent(Intent);                 // застосунок запущено за посиланням
        }

        protected override void OnNewIntent(Android.Content.Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);                 // застосунок уже працював, прийшло посилання
        }

        private static void HandleIntent(Android.Content.Intent? intent)
        {
            var data = intent?.Data;
            if (intent?.Action != Android.Content.Intent.ActionView || data is null) return;

            if (Uri.TryCreate(data.ToString(), UriKind.Absolute, out var uri))
                IPlatformApplication.Current?.Services.GetService<IDeepLinkService>()?.Receive(uri);
        }
    }
}
