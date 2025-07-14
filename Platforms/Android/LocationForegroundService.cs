using Android.App;// Пространство имён для базовых Android-компонентов (Activity, Service и др.)
using Android.Content; // Для работы с Intent, Context и т.д.
using Android.Content.PM;// Для работы с разрешениями (Permission)
using Android.Locations;// Для работы с локацией (LocationManager, ILocationListener)
using Android.OS;// Для доступа к Android OS API (Bundle, IBinder и др.)
using Android.Runtime;// Для GeneratedEnum и совместимости с Java-кодом
using Android.Util;
using AndroidX.Core.App;// Для NotificationCompat (создание уведомлений)
using Firebase.Database;// Для работы с Firebase Realtime Database
using Firebase.Database.Query; // Для запросов к Firebase (Child, PutAsync и др.)
using Microsoft.Maui.Storage;// Для Preferences (кроссплатформенное локальное хранилище)
using System;// Базовые типы C# (DateTime и др.)
using System.Reactive;
using Location = Android.Locations.Location;// Явное указание, что Location из Android.Locations

namespace MauiGpsDemo.Platforms.Android
{

    // Атрибут Service указывает, что это сервис, который будет работать в фоне
    // Атрибут указывает, что сервис - foreground и будет использовать GPS (TypeLocation)
    [Service(ForegroundServiceType = ForegroundService.TypeLocation)]
    public class LocationForegroundService : Service, ILocationListener
    {
        // // Объявляем менеджер локаций, который будет использоваться для доступа к сервисам геолокации устройства.
        LocationManager locationManager;
        // Строка для хранения текущего провайдера локации (например, GPS).
        string locationProvider;
        // Экземпляр клиента для доступа к Firebase Realtime Database.
        FirebaseClient firebase = new FirebaseClient("https://gpsdemo-5820b-default-rtdb.firebaseio.com/");
        
        // Метод обязательный для сервиса. В данном случае он не используется, поэтому возвращает null.
        public override IBinder OnBind(Intent intent) => null;

        // Метод вызывается при создании сервиса (один раз).
        public override void OnCreate()
        {
            base.OnCreate();// Вызов базовой реализации.
           // Получаем системный сервис для работы с локацией.
            locationManager = (LocationManager)GetSystemService(LocationService);
            //По умолчанию используем GPS-провайдер.
            locationProvider = LocationManager.GpsProvider;

            // === Создание канала уведомлений для Android 8+ ===
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                string channelId = "location_channel";
                string channelName = "Трекинг геолокации";
                string channelDescription = "Уведомления о фоновом отслеживании координат";
                var channel = new NotificationChannel(channelId, channelName, NotificationImportance.Default)
                {
                    Description = channelDescription
                };
                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }



        }
        // Главный метод жизненного цикла сервиса — вызывается при каждом запуске или перезапуске.
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // Получаем из intent идентификатор ребёнка, если не передан — используем "child1".
            string childId = intent.GetStringExtra("childId") ?? "child1";
            // Создаём уведомление для foreground-сервиса.
            var notification = new NotificationCompat.Builder(this, "location_channel")
                .SetContentTitle("Отслеживание местоположения")
                .SetContentText("Координаты отправляются в фоне")
                .SetSmallIcon(Resource.Drawable.abc_btn_radio_material) // <- стандартная иконка!
                .Build();
            // Переводим сервис в режим foreground с этим уведомлением (иначе Android может убить сервис).
            StartForeground(1, notification);

            // Получаем список включённых провайдеров локации (GPS, Network и т.д.).
            var availableProviders = locationManager.GetProviders(true); // true - только включённые
            // Если доступен GPS-провайдер, подписываемся на его обновления.
            if (availableProviders.Contains(LocationManager.GpsProvider))
            {
                // Запрашиваем обновления координат каждые 10 секунд (10000 мс), без минимального смещения (0 метров), this — текущий listener.
                locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 10000, 0, this);
            }
            // Если GPS нет, но есть сетевой провайдер — используем его.
            else if (availableProviders.Contains(LocationManager.NetworkProvider))
            {
                locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 10000, 0, this);
            }
            else
            {

                // Нет доступных провайдеров
                Log.Warn("GPS", "Нет доступных провайдеров локации!");
            }
            // Проверяем разрешения на доступ к местоположению
            // Проверяем доступные провайдеры (GPS и Network)
            //  locationManager.RequestLocationUpdates(locationProvider, 10000, 0, this);


            // Сохраняем идентификатор ребёнка в Preferences (локальное хранилище ключ-значение).
            Preferences.Default.Set("CurrentChildId", childId);
            // Возвращаем флаг, что сервис должен быть перезапущен системой, если его убьют.
            return StartCommandResult.Sticky;
        }

        public void OnLocationChanged(Location location)
        {
            string childId = Preferences.Default.Get("CurrentChildId", "child1");

            // Сохраняем координаты локально
            Preferences.Default.Set("LastLat", location.Latitude);
            Preferences.Default.Set("LastLng", location.Longitude);
            Preferences.Default.Set("LastTime", DateTime.UtcNow.ToString("o"));




            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await firebase
                    .Child("locations")
                    .Child(childId)
                    .PutAsync(new
                    {
                        lat = location.Latitude,
                        lng = location.Longitude,
                        time = DateTime.UtcNow.ToString("o")
                    });
            });
        }

        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras) { }

      
    }
}


//Подробное объяснение каждой строки
//C#
//public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
//{
//    Это метод жизненного цикла Android - сервиса.

//Он вызывается каждый раз при запуске сервиса(или когда система его перезапускает).
//В него приходит Intent — объект с дополнительными данными(например, с идентификатором ребёнка).

//    // Получаем из intent идентификатор ребёнка, если не передан — используем "child1".
//    string childId = intent.GetStringExtra("childId") ?? "child1";
//Извлекаем идентификатор ребёнка:

//Приложение может передать с intent ключ "childId".
//Если такого нет — используем строку "child1" по умолчанию.
//Это позволяет сервису знать, для какого ребёнка отправлять координаты.

// Создаём уведомление для foreground-сервиса.
//var notification = new NotificationCompat.Builder(this, "location_channel")
//    .SetContentTitle("Отслеживание местоположения")
//    .SetContentText("Координаты отправляются в фоне")
//    .SetSmallIcon(Resource.Drawable.abc_btn_radio_material) // <- стандартная иконка!
//    .Build();
//Создаём уведомление:

//Foreground - сервис ОБЯЗАТЕЛЬНО должен показывать уведомление в статус-баре.
//Здесь оно говорит: "Отслеживание местоположения"(заголовок), "Координаты отправляются в фоне"(текст).
//.SetSmallIcon(...) — задаёт иконку уведомления (можно свою, у тебя — стандартная).
//.Build() — финализирует объект уведомления.

// Переводим сервис в режим foreground с этим уведомлением (иначе Android может убить сервис).
//StartForeground(1, notification);
//Делаем сервис foreground:

//Без этого сервис почти всегда будет убиваться системой через несколько минут (особенно на новых Android).
//В foreground сервисе Android гарантирует работу и показывает твоё уведомление пользователю.

// Получаем список включённых провайдеров локации (GPS, Network и т.д.).
//var availableProviders = locationManager.GetProviders(true); // true - только включённые
//Получаем список источников локации (провайдеров):

//Например, GPS, Network (по Wi - Fi и мобильной сети).
//true — только те, которые реально включены пользователем в настройках.

// Если доступен GPS-провайдер, подписываемся на его обновления.
//if (availableProviders.Contains(LocationManager.GpsProvider))
//{
//    // Запрашиваем обновления координат каждые 10 секунд (10000 мс), без минимального смещения (0 метров), this — текущий listener.
//    locationManager.RequestLocationUpdates(LocationManager.GpsProvider, 10000, 0, this);
//}
//Если есть GPS:

//Подписываемся на обновления координат от GPS.
//Каждые 10 секунд (10000 миллисекунд), без минимального сдвига (0 метров).
//this — сервис будет слушателем (реализует интерфейс ILocationListener).

// Если GPS нет, но есть сетевой провайдер — используем его.
//    else if (availableProviders.Contains(LocationManager.NetworkProvider))
//{
//    locationManager.RequestLocationUpdates(LocationManager.NetworkProvider, 10000, 0, this);
//}
//Если GPS нет, но есть Network (например, по Wi-Fi):

//Подписываемся на обновления координат от сетевого провайдера.

//else
//{
//    // Нет доступных провайдеров
//    Log.Warn("GPS", "Нет доступных провайдеров локации!");
//}
//Если нет ни одного провайдера:

//Просто пишем предупреждение в лог — сервис не сможет получать координаты.

// Сохраняем идентификатор ребёнка в Preferences (локальное хранилище ключ-значение).
//Preferences.Default.Set("CurrentChildId", childId);
//Сохраняем id ребёнка в локальное хранилище приложения:

//Позволяет потом в других методах (например, при отправке координат) знать, для какого ребёнка отправлять данные.
// Возвращаем флаг, что сервис должен быть перезапущен системой, если его убьют.
//return StartCommandResult.Sticky;
//}
//Говорим системе:

//Если сервис был убит системой, его надо перезапустить с тем же intent (и childId).
//Это важно для фонового отслеживания!

//Сервис запускается → получает идентификатор ребёнка.
//Создаёт уведомление и становится foreground-сервисом.
//Выбирает лучший из доступных провайдеров локации и подписывается на их обновления.
//Сохранит id ребёнка для последующей отправки координат.
//Гарантирует перезапуск (если система выгрузила сервис).
