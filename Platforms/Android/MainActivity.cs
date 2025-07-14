using Android.App;
using Android.Content.PM;
using Android.OS;



namespace MauiGpsDemo
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("location_channel", "Location", NotificationImportance.Default)
                {
                    Description = "Отслеживание координат"
                };
                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }

            // Запрос разрешения Foreground Service Location для Android 14+
            if ((int)Build.VERSION.SdkInt >= 34) // 34 = Android 14
            {
                if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                        this,
                        "android.permission.FOREGROUND_SERVICE_LOCATION"
                    ) != Android.Content.PM.Permission.Granted)
                  {
                    AndroidX.Core.App.ActivityCompat.RequestPermissions(
                        this,
                        new string[] { "android.permission.FOREGROUND_SERVICE_LOCATION" },
                        2001 // requestCode, любое число
                    );
                }
            }
        }




    }
}
