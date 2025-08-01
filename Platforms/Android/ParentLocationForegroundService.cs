﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
using Android.Util;

namespace MauiGpsDemo.Platforms.Android
{
    [Service(ForegroundServiceType = ForegroundService.TypeLocation)]
    public class ParentLocationForegroundService : Service
    {

       // FirebaseClient firebase = new FirebaseClient("https://gpsdemo-5820b-default-rtdb.firebaseio.com/");

        /// /////////////////////////////////////////////////////////////////////////////
        // ДОБАВИТЬ в сервисах вместо поля:
        FirebaseClient firebase => MauiGpsDemo.MainPage.firebase;


        ////////////////////////////////////////////////////////////////////////////////


        Timer pollingTimer;
        string trackingChildId = "child1";
        string channelId = "parent_location_channel";
        public override IBinder OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Трекинг ребёнка", NotificationImportance.High)
                {
                    Description = "Фоновое получение координат ребёнка"
                };
                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            trackingChildId = intent.GetStringExtra("childId") ?? "child1";

            var notification = new NotificationCompat.Builder(this, channelId)
                .SetContentTitle("Поиск ребёнка")
                .SetContentText($"Отслеживание координат {trackingChildId}")
                .SetSmallIcon(Resource.Drawable.abc_btn_radio_material)
                 .SetPriority((int)NotificationPriority.Max)
                .Build();

            StartForeground(2, notification);

            pollingTimer = new Timer(20000); // 5 секунд
            pollingTimer.Elapsed += PollingTimer_Elapsed;
            pollingTimer.Start();

            Preferences.Default.Set("ParentTrackingChildId", trackingChildId);

            return StartCommandResult.Sticky;
        }

        //private async void PollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    try
        //    {
        //        // АНДРЕЙ МЕНЯЙ ЗДЕСЬ — добавь эту строку!
        //        await MauiGpsDemo.MainPage.EnsureFirebaseTokenAsync();


        //        var loc = await firebase
        //            .Child("locations")
        //            .Child(trackingChildId)
        //            .OnceSingleAsync<GpsLocation>();

        //        if (loc != null)
        //        {
        //            Preferences.Default.Set("ParentLastLat", loc.lat);
        //            Preferences.Default.Set("ParentLastLng", loc.lng);
        //            Preferences.Default.Set("ParentLastTime", loc.time ?? DateTime.UtcNow.ToString("o"));
        //            Preferences.Default.Set("ParentLastBattery", loc.battery); // добавлено 
        //        }
        //    }
        //    catch { /* Можно добавить логирование */ }
        //}


        private async void PollingTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await MauiGpsDemo.MainPage.EnsureFirebaseTokenAsync();

                var loc = await firebase
                    .Child("locations")
                    .Child(trackingChildId)
                    .OnceSingleAsync<GpsLocation>();

                if (loc != null)
                {
                    Preferences.Default.Set("ParentLastLat", loc.lat);
                    Preferences.Default.Set("ParentLastLng", loc.lng);
                    Preferences.Default.Set("ParentLastTime", loc.time ?? DateTime.UtcNow.ToString("o"));
                    Preferences.Default.Set("ParentLastBattery", loc.battery);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("GPS", $"Parent Firebase read failed: {ex}");

                // Если ошибка связана с невалидным токеном или правами — пробуем форс-обновить токен и повторить один раз
                if (ex.Message.Contains("401") || ex.Message.Contains("Permission denied"))
                {
                    try
                    {
                        Log.Warn("GPS", "Parent: Attempting force token refresh after 401/Permission denied");
                        await MauiGpsDemo.MainPage.EnsureFirebaseTokenAsync(true);

                        var loc = await firebase
                            .Child("locations")
                            .Child(trackingChildId)
                            .OnceSingleAsync<GpsLocation>();

                        if (loc != null)
                        {
                            Preferences.Default.Set("ParentLastLat", loc.lat);
                            Preferences.Default.Set("ParentLastLng", loc.lng);
                            Preferences.Default.Set("ParentLastTime", loc.time ?? DateTime.UtcNow.ToString("o"));
                            Preferences.Default.Set("ParentLastBattery", loc.battery);
                        }
                    }
                    catch (Exception ex2)
                    {
                        Log.Warn("GPS", $"Parent Firebase retry failed: {ex2}");
                    }
                }
            }
        }


        public override void OnDestroy()
        {
            pollingTimer?.Stop();
            pollingTimer?.Dispose();
            pollingTimer = null; // Обнуляем ссылку на таймер
            base.OnDestroy();
        }

        class GpsLocation
        {
            public double lat { get; set; }
            public double lng { get; set; }
            public string time { get; set; }

            public int battery { get; set; } // добавлено
        }


       
    }
}
