using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Android.Provider;
using Microsoft.Maui.Storage;


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
                var channel = new NotificationChannel("location_channel", "Location", NotificationImportance.High)
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

           
                ShowBatteryOptimizationDialog();
                ShowAutostartDialog();
             




        }

        // Обработка результата запроса разрешений - добавлено
        void ShowAutostartDialog()
        {
            try
            {
                new AlertDialog.Builder(this)
                    .SetTitle("Разрешить автозапуск")
                    .SetMessage("Для корректной работы в фоне разрешите автозапуск приложения. Открыть настройки?")
                    .SetPositiveButton("Да", (s, e) =>
                    {
                        try
                        {
                            Intent intent = new Intent();
                            intent.SetComponent(new ComponentName("com.miui.securitycenter", "com.miui.permcenter.autostart.AutoStartManagementActivity"));
                            StartActivity(intent);
                        }
                        catch
                        {
                            Toast.MakeText(this, "Меню автозапуска не найдено.", ToastLength.Short).Show();
                        }
                    })
                    .SetNegativeButton("Нет", (s, e) => { })
                    .Show();
            }
            catch { }
        }

        void ShowBatteryOptimizationDialog()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                try
                {
                    string packageName = PackageName;
                    var pm = (PowerManager)GetSystemService(PowerService);
                    if (!pm.IsIgnoringBatteryOptimizations(packageName))
                    {
                        new AlertDialog.Builder(this)
                            .SetTitle("Отключить оптимизацию батареи")
                            .SetMessage("Для стабильной работы GPS-трекинга отключите оптимизацию батареи для приложения. Открыть настройки?")
                            .SetPositiveButton("Да", (s, e) =>
                            {
                                try
                                {
                                    Intent intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                                    intent.SetData(Android.Net.Uri.Parse("package:" + packageName));
                                    StartActivity(intent);
                                }
                                catch
                                {
                                    Toast.MakeText(this, "Ошибка открытия настроек оптимизации", ToastLength.Short).Show();
                                }
                            })
                            .SetNegativeButton("Нет", (s, e) => { })
                            .Show();
                    }
                }
                catch { }
            }
        }




        ////////////////////////////////////////////////////

    }
}
