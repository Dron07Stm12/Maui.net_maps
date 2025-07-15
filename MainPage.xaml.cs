using Firebase.Auth; // <-- В начало файла
using Microsoft.Maui.Controls.Maps;    // Для карт  
using Microsoft.Maui.Devices.Sensors;  // Для Geolocation
using Microsoft.Maui.Maps;
using System;
using System.Timers;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.Maui;                  // Базовые типы
using Microsoft.Maui.ApplicationModel; // Для PermissionStatus
using Microsoft.Maui.Controls;
   #if ANDROID
   using Android.Content;
   using MauiGpsDemo.Platforms.Android;
  #endif
namespace MauiGpsDemo
{
    public partial class MainPage : ContentPage
    {
        // Строка с URL вашей базы Firebase. Используется и на клиенте, и в сервисах.
        private const string firebaseUrl = "https://gpsdemo-5820b-default-rtdb.firebaseio.com/";
        // Не используется в текущем коде (оставлено для возможного Firebase listener'а)
        private IDisposable firebaseListener;
        // Список точек маршрута ребёнка (накапливается для "Следа" на карте).
        private List<Location> childLocations = new();
        // Флаг: показываем ли сейчас "след" на карте (true — показан, false — нет).
        private bool isTrailShown = false;

        // === ДОБАВЛЕНО: таймер для автообновления карты родителя ===
        private System.Timers.Timer _parentUpdateTimer;
        // Таймер для debounce (отложенного реагирования на ввод в ParentIdEntry).
        private System.Timers.Timer _debounceTimer;


        // === Новый обработчик для ChildIdEntry ===
        private System.Timers.Timer _childDebounceTimer; // отдельный debounce-таймер для ребёнка


        public MainPage()
        {
            InitializeComponent();// MAUI инициализация разметки и элементов.
            ShowPanels(null); // Прячем обе панели (или показываем нужную).
            ////////////////////////////
            //  Подписка на изменение текста в ParentIdEntry ===
            ParentIdEntry.TextChanged += ParentIdEntry_TextChanged;// Подписка: если родитель меняет ID ребёнка — срабатывает debounce-логика.
                                                                  
          
            //  подписка на изменение текста в ChildIdEntry
            ChildIdEntry.TextChanged += ChildIdEntry_TextChanged;


        }


        private void ChildIdEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Сбрасываем старый debounce-таймер
            _childDebounceTimer?.Stop();
            _childDebounceTimer?.Dispose();

            _childDebounceTimer = new System.Timers.Timer(800);
            _childDebounceTimer.AutoReset = false;
            _childDebounceTimer.Elapsed += (s, ev) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ChildPanel.IsVisible) // Только если панель ребёнка активна
                    {
                        var childId = ChildIdEntry.Text?.Trim() ?? "child1";
#if ANDROID
                        StopChildLocationService();
                        StartChildLocationService(childId);
#endif
                    }
                });
            };
            _childDebounceTimer.Start();
        }







        /// ////////////////////////////////////////////////

        // === ДОБАВЛЕНО: обработчик изменения childId родителя ===
        //        private void ParentIdEntry_TextChanged(object sender, TextChangedEventArgs e)
        //        {
        //            if (ParentPanel.IsVisible) // Только если панель родителя активна
        //            {
        //                var childId = ParentIdEntry.Text?.Trim() ?? "child1";
        //#if ANDROID
        //                StopParentLocationService();     // Остановить старый сервис
        //                StartParentLocationService(childId); // Запустить новый сервис с новым childId
        //#endif
        //                // Перезапустить таймер (он всегда использует актуальный childId)
        //                _parentUpdateTimer?.Stop();
        //                _parentUpdateTimer = new System.Timers.Timer(5000);
        //                _parentUpdateTimer.Elapsed += (s, e2) =>
        //                {
        //                    MainThread.BeginInvokeOnMainThread(() => UpdateParentFromPreferences(childId));
        //                };
        //                _parentUpdateTimer.AutoReset = true;
        //                _parentUpdateTimer.Start();
        //                UpdateParentFromPreferences(childId); // сразу обновить
        //            }
        //        }

        /////////////////////////////////////////////////////



        //
        /// <summary>
        ///  Метод-обработчик, вызывается каждый раз, когда изменяется текстовое поле ParentIdEntry (то есть пользователь меняет ID ребёнка в режиме родителя).

        private void ParentIdEntry_TextChanged(object sender, TextChangedEventArgs e)                                                                             
        {
            // Сбрасываем старый debounce-таймер
            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();

            //  создаётся экземпляр таймера (_debounceTimer), который настроен на срабатывание через 800 миллисекунд.
            _debounceTimer = new System.Timers.Timer(800);
            //таймер сработает только один раз (не будет зацикливаться).
            _debounceTimer.AutoReset = false;
            //Подписывается на событие Elapsed (срабатывает, когда таймер истечёт).
            _debounceTimer.Elapsed += (s, ev) =>
            {
                // Этот код выполнится через 800 мс после последнего изменения текста в ParentIdEntry.
                // Используем MainThread для обновления UI, так как таймер работает в фоновом потоке.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ParentPanel.IsVisible)//активна ли панель родителя (ParentPanel.IsVisible). 
                    {
                        //Получаем текущее значение поля ParentIdEntry (ID ребёнка)
                        var childId = ParentIdEntry.Text?.Trim() ?? "child1";
#if ANDROID              
                        // Остановить старый сервис и запустить новый с новым childId   
                        // (это нужно, чтобы сервис всегда работал с актуальным ID ребёнка).
                        StopParentLocationService();
                        StartParentLocationService(childId);
#endif     
                        //Останавливает предыдущий таймер обновления карты
                        _parentUpdateTimer?.Stop();
                        //Создаёт новый таймер на 5 секунд
                        _parentUpdateTimer = new System.Timers.Timer(5000);

                        //Подписывается на событие Elapsed — при каждом срабатывании вызывает UpdateParentFromPreferences(childId)
                        //в основном потоке (обновляет карту и данные).
                        _parentUpdateTimer.Elapsed += (s2, e2) =>
                        {
                            MainThread.BeginInvokeOnMainThread(() => UpdateParentFromPreferences(childId));
                        };
                        //Устанавливает таймер на автообновление (каждые 5 секунд)
                        _parentUpdateTimer.AutoReset = true;
                        //Запускает таймер  
                        _parentUpdateTimer.Start();
                        // // Сразу обновляем карту и данные родителя
                        UpdateParentFromPreferences(childId);
                    }
                });
            };
            //Запускает новый debounce - таймер(_debounceTimer.Start()),
            //который сработает через 800 мс, если пользователь не будет вводить новые символы.

            _debounceTimer.Start();
        }
        private void OnChildModeClicked(object sender, EventArgs e)
        {
            ShowPanels("child");
            _ = RequestLocationAndStartServiceAsync();
        }

        private void OnParentModeClicked(object sender, EventArgs e)
        {
            ShowPanels("parent");
            StopChildLocationService();
        }

        // === ИЗМЕНЕНО: ShowPanels для режима РОДИТЕЛЯ используем сервис и таймер, не ListenToChildLocation ===
        private void ShowPanels(string mode)
        {
            ChildPanel.IsVisible = mode == "child";
            ParentPanel.IsVisible = mode == "parent";
            if (mode == "parent")
            {
                var childId = ParentIdEntry.Text?.Trim() ?? "child1";
#if ANDROID
                StartParentLocationService(childId);
#endif
                // НЕ используем ListenToChildLocation(childId);
                // Запускаем таймер для автообновления карты из Preferences:
                _parentUpdateTimer?.Stop();
                _parentUpdateTimer = new System.Timers.Timer(5000); // 5 секунд
                _parentUpdateTimer.Elapsed += (s, e) =>
                {
                    MainThread.BeginInvokeOnMainThread(() => UpdateParentFromPreferences(childId));
                };
                _parentUpdateTimer.AutoReset = true;
                _parentUpdateTimer.Start();
                UpdateParentFromPreferences(childId); // сразу обновить
            }
            else
            {
#if ANDROID
                StopParentLocationService();
#endif
                firebaseListener?.Dispose();
                // Остановить таймер при выходе из режима родителя
                _parentUpdateTimer?.Stop();
                _parentUpdateTimer = null;
            }
        }

        // === ДОБАВЛЕНО: методы запуска и остановки ForegroundService родителя ===
#if ANDROID
        private void StartParentLocationService(string childId)
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(ParentLocationForegroundService));
            intent.PutExtra("childId", childId ?? "child1");
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);
        }

        private void StopParentLocationService()
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(ParentLocationForegroundService));
            context.StopService(intent);
        }
#endif

        // ===== Фоновое отслеживание (ForegroundService) =====

        private async Task RequestLocationAndStartServiceAsync()
        {
            // Запрашиваем разрешение на геолокацию всегда (в фоне)
            var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Внимание", "Нет разрешения на фоновую геолокацию!", "ОК");
                return;
            }

#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                var pm = (Android.OS.PowerManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.PowerService);
                if (!pm.IsIgnoringBatteryOptimizations(Android.App.Application.Context.PackageName))
                {
                    var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
                    intent.SetFlags(Android.Content.ActivityFlags.NewTask);
                    Android.App.Application.Context.StartActivity(intent);
                }
            }
#endif

            StartChildLocationService(ChildIdEntry.Text?.Trim() ?? "child1");
        }

        public void StartChildLocationService(string childId)
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundService));
            intent.PutExtra("childId", childId ?? "child1");
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                context.StartForegroundService(intent);
            else
                context.StartService(intent);
#endif
        }

        public void StopChildLocationService()
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(LocationForegroundService));
            context.StopService(intent);
#endif
        }

        // ==== РЕЖИМ РОДИТЕЛЯ ====

        private class GpsLocation
        {
            public double lat { get; set; }
            public double lng { get; set; }
            public string? time { get; set; }
            public int battery { get; set; } // <-- ДОБАВЬ ЭТУ СТРОКУ!
        }


        //private void OnTrackChildClicked(object sender, EventArgs e)
        //{
        //    var childId = ParentIdEntry.Text?.Trim() ?? "child1";
        //    UpdateParentFromPreferences(childId);
        //}

        // === ДОБАВЛЕНО: обновление карты родителя из Preferences ===

        // === ИЗМЕНЕНО: OnTrackChildClicked теперь тоже перезапускает сервис и таймер ===
        private void OnTrackChildClicked(object sender, EventArgs e)
        {
            var childId = ParentIdEntry.Text?.Trim() ?? "child1";
#if ANDROID
            StopParentLocationService();
            StartParentLocationService(childId);
#endif
            // Перезапустить таймер (повторяет логику из TextChanged для надёжности)
            _parentUpdateTimer?.Stop();
            _parentUpdateTimer = new System.Timers.Timer(5000);
            _parentUpdateTimer.Elapsed += (s, e2) =>
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateParentFromPreferences(childId));
            };
            _parentUpdateTimer.AutoReset = true;
            _parentUpdateTimer.Start();
            UpdateParentFromPreferences(childId);
        }

        private void UpdateParentFromPreferences(string childId)
        {
            double lat = Preferences.Default.Get("ParentLastLat", 0.0);
            double lng = Preferences.Default.Get("ParentLastLng", 0.0);
            string time = Preferences.Default.Get("ParentLastTime", "нет данных");         
            int battery = Preferences.Default.Get("ParentLastBattery", -1);
          
            var position = new Location(lat, lng);

            // Добавляем точку в историю, если новая
            if (childLocations.Count == 0 ||
                childLocations.Last().Latitude != position.Latitude || childLocations.Last().Longitude != position.Longitude)
            {
                childLocations.Add(position);
            }

            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200)));

            if (!isTrailShown)
            {
                MyMap.Pins.Clear();
                MyMap.Pins.Add(new Pin
                {
                    Label = $"Ребёнок: {childId}",
                    Location = position,
                    Type = PinType.Place,
                });
            }

            // Преобразуем время из UTC в локальное:
            string localTimeStr = time;
            try
            {
                if (!string.IsNullOrEmpty(time) && time != "нет данных")
                {
                    DateTime utcTime = DateTime.Parse(time, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    DateTime localTime = utcTime.ToLocalTime();
                    localTimeStr = localTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            catch
            {
                localTimeStr = time;
            }

            ParentLocationLabel.Text = $"Широта: {lat}\nДолгота: {lng}\nДата: {localTimeStr}";
          //  ParentBatteryLabel.Text = battery >= 0 ? $"Батарея: {battery}%" : "Батарея: нет данных";

            // Обработка отображения батареи
            if (battery >= 0 && battery <= 100)
            {
                Preferences.Default.Set("ParentLastBatteryValid", battery);
            }
            int lastValidBattery = Preferences.Default.Get("ParentLastBatteryValid", -1);

            if (lastValidBattery == 0)
                ParentBatteryLabel.Text = "Батарея: 0% (возможно, устройство выключено)";
            else if (lastValidBattery > 0)
                ParentBatteryLabel.Text = $"Батарея: {lastValidBattery}%";
            else
                ParentBatteryLabel.Text = "Батарея: нет данных";




        }

   

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopChildLocationService();
#if ANDROID
            StopParentLocationService();
#endif
            firebaseListener?.Dispose();
            // Остановить таймер при закрытии страницы
            _parentUpdateTimer?.Stop();
            _parentUpdateTimer = null;
        }

        private void OnGetLocationClicked(object sender, EventArgs e)
        {
            try
            {
                double lat = Preferences.Default.Get("LastLat", 0.0);
                double lng = Preferences.Default.Get("LastLng", 0.0);
                string time = Preferences.Default.Get("LastTime", "нет данных");

                string localTimeStr = time;
                try
                {
                    if (!string.IsNullOrEmpty(time) && time != "нет данных")
                    {
                        DateTime utcTime = DateTime.Parse(time, null, System.Globalization.DateTimeStyles.RoundtripKind);
                        DateTime localTime = utcTime.ToLocalTime();
                        localTimeStr = localTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                catch
                {
                    localTimeStr = time;
                }

                ChildLocationLabel.Text = $"Широта: {lat}, Долгота: {lng}\nДата: {localTimeStr}";
            }
            catch (Exception ex)
            {
                ChildLocationLabel.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void OnSatelliteClicked(object sender, EventArgs e)
        {
            if (MyMap.MapType == MapType.Street) { MyMap.MapType = MapType.Satellite; }
            else { MyMap.MapType = MapType.Street; }
        }

        private void OnShowPointsClicked(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!isTrailShown)
                {
                    MyMap.Pins.Clear();
                    int i = 1;
                    foreach (var pos in childLocations)
                    {
                        MyMap.Pins.Add(new Pin
                        {
                            Label = $"Точка {i}",
                            Location = pos,
                            Type = PinType.SavedPin
                        });
                        i++;
                    }
                    if (childLocations.Count > 0)
                    {
                        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(childLocations.Last(), Distance.FromMeters(200)));
                    }
                    isTrailShown = true;
                    ((Button)sender).Text = "Скрыть след";
                }
                else
                {
                    MyMap.Pins.Clear();
                    if (childLocations.Count > 0)
                    {
                        MyMap.Pins.Add(new Pin
                        {
                            Label = $"Ребёнок",
                            Location = childLocations.Last(),
                            Type = PinType.Place,
                        });
                    }
                    isTrailShown = false;
                    ((Button)sender).Text = "След";
                }
            });
        }
    }
}

//namespace MauiGpsDemo
//{
//    public partial class MainPage : ContentPage
//    {

//        private const string firebaseUrl = "https://gpsdemo-5820b-default-rtdb.firebaseio.com/";
//        private IDisposable firebaseListener;
//        private List<Location> childLocations = new();
//        private bool isTrailShown = false;

//        // === ДОБАВЛЕНО: таймер для автообновления карты родителя ===
//        private System.Timers.Timer _parentUpdateTimer;



//        public MainPage()
//        {
//            InitializeComponent();
//            ShowPanels(null);
//        }

//        private void OnChildModeClicked(object sender, EventArgs e)
//        {
//            ShowPanels("child");
//            _ = RequestLocationAndStartServiceAsync();
//        }

//        private void OnParentModeClicked(object sender, EventArgs e)
//        {
//            ShowPanels("parent");
//            StopChildLocationService();
//        }

//        //private void ShowPanels(string mode)
//        //{
//        //    ChildPanel.IsVisible = mode == "child";
//        //    ParentPanel.IsVisible = mode == "parent";
//        //}

//        private void ShowPanels(string mode)
//        {
//            ChildPanel.IsVisible = mode == "child";
//            ParentPanel.IsVisible = mode == "parent";
//            if (mode == "parent")
//            {
//                var childId = ParentIdEntry.Text?.Trim() ?? "child1";
//                ListenToChildLocation(childId);
//            }
//            else
//            {
//                firebaseListener?.Dispose();
//            }
//        }





//        // ===== Фоновое отслеживание (ForegroundService) =====

//        private async Task RequestLocationAndStartServiceAsync()
//        {
//            // Запрашиваем разрешение на геолокацию всегда (в фоне)
//            var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
//            if (status != PermissionStatus.Granted)
//            {
//                await DisplayAlert("Внимание", "Нет разрешения на фоновую геолокацию!", "ОК");
//                return;
//            }

//            // === Просим пользователя отключить оптимизацию батареи ===
//#if ANDROID
//            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
//            {
//                var pm = (Android.OS.PowerManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.PowerService);
//                if (!pm.IsIgnoringBatteryOptimizations(Android.App.Application.Context.PackageName))
//                {
//                    var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
//                    intent.SetData(Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
//                    intent.SetFlags(Android.Content.ActivityFlags.NewTask);
//                    Android.App.Application.Context.StartActivity(intent);
//                }
//            }
//#endif




//            // Запрашиваем разрешение на Foreground Service Location (Android 14+) с передачей параметра "ChildIdEntry.Text" или по умолчанию "child1"
//            StartChildLocationService(ChildIdEntry.Text?.Trim() ?? "child1");
//        }

//        public void StartChildLocationService(string childId)
//        {
//#if ANDROID
//            //Получаем глобальный context Android-приложения

//            var context = Android.App.Application.Context;
//            //— context — это объект, представляющий текущее приложение или компонент. Он необходим, чтобы Android знал, "откуда" исходит запрос
//            //	Второй параметр — typeof(LocationForegroundService) — это тип сервиса, который мы хотим запустить.
//            var intent = new Intent(context, typeof(LocationForegroundService));
//            // Передаём идентификатор ребёнка в сервис через Intent - это позволяет сервису знать, для какого ребёнка он должен отслеживать местоположение
//            //добавить дополнительный параметр
//            intent.PutExtra("childId", childId ?? "child1");

//            //  context.StartForegroundService(intent);

//            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
//                context.StartForegroundService(intent);
//            else
//                context.StartService(intent);

//#endif
//        }

//        public void StopChildLocationService()
//        {
//#if ANDROID
//            var context = Android.App.Application.Context;
//            var intent = new Intent(context, typeof(LocationForegroundService));
//            context.StopService(intent);
//#endif
//        }

//        // ==== РЕЖИМ РОДИТЕЛЯ ====

//        private class GpsLocation
//        {
//            public double lat { get; set; }
//            public double lng { get; set; }
//            public string? time { get; set; }
//        }

//        private void OnTrackChildClicked(object sender, EventArgs e)
//        {
//            var childId = ParentIdEntry.Text?.Trim() ?? "child1";
//            ListenToChildLocation(childId);
//        }

//        private void ListenToChildLocation(string childId)
//        {
//            firebaseListener?.Dispose();
//            var firebase = new FirebaseClient(firebaseUrl);

//            // 1. Сначала получить текущее значение
//            MainThread.InvokeOnMainThreadAsync(async () =>
//            {
//                try
//                {
//                    var current = await firebase
//                        .Child("locations")
//                        .Child(childId)
//                        .OnceSingleAsync<GpsLocation>();

//                    if (current != null)
//                        UpdateParentMap(current, childId);
//                    else
//                        ParentLocationLabel.Text = "Местоположение не получено.";
//                }
//                catch
//                {
//                    ParentLocationLabel.Text = "Ошибка чтения данных.";
//                }
//            });

//            // 2. Потом слушать обновления (только изменения)
//            firebaseListener = firebase
//                .Child("locations")
//                .Child(childId)
//                .AsObservable<GpsLocation>()
//                .Subscribe(item =>
//                {
//                    if (item.Object != null)
//                    {
//                        UpdateParentMap(item.Object, childId);
//                    }
//                });
//        }

//        //private void UpdateParentMap(GpsLocation loc, string childId)
//        //{
//        //    MainThread.BeginInvokeOnMainThread(() =>
//        //    {
//        //        var position = new Location(loc.lat, loc.lng);
//        //        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200)));
//        //        MyMap.Pins.Clear();
//        //        MyMap.Pins.Add(new Pin
//        //        {
//        //            Label = $"Ребёнок: {childId}",
//        //            Location = position,
//        //            Type = PinType.Place,
//        //        });
//        //        ParentLocationLabel.Text = $"Широта: {loc.lat}\nДолгота: {loc.lng}\nДата: {loc.time}";
//        //    });
//        //}


//        private void UpdateParentMap(GpsLocation loc, string childId)
//        {
//            MainThread.BeginInvokeOnMainThread(() =>
//            {

//                var position = new Location(loc.lat, loc.lng);

//                // Добавляем точку в историю, если новая
//                if (childLocations.Count == 0 ||
//                    childLocations.Last().Latitude != position.Latitude || childLocations.Last().Longitude != position.Longitude)
//                {
//                    childLocations.Add(position);
//                }

//                MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200)));

//                if (!isTrailShown)
//                {
//                    MyMap.Pins.Clear(); // Очищаем пины, если показываем тропу
//                    MyMap.Pins.Add(new Pin
//                    {
//                        Label = $"Ребёнок: {childId}",
//                        Location = position,
//                        Type = PinType.Place,
//                    });
//                }
//                // Преобразуем время из UTC в локальное:
//                string localTimeStr = loc.time;
//                try
//                {
//                    if (!string.IsNullOrEmpty(loc.time))
//                    {
//                        DateTime utcTime = DateTime.Parse(loc.time, null, System.Globalization.DateTimeStyles.RoundtripKind);
//                        DateTime localTime = utcTime.ToLocalTime();
//                        localTimeStr = localTime.ToString("yyyy-MM-dd HH:mm:ss"); // Можно выбрать любой формат
//                    }
//                }
//                catch
//                {
//                    localTimeStr = loc.time; // Если парсинг не удался, оставить как есть
//                }




//                ParentLocationLabel.Text = $"Широта: {loc.lat}\nДолгота: {loc.lng}\nДата: {localTimeStr}";
//            });
//        }




//        protected override void OnDisappearing()
//        {
//            base.OnDisappearing();
//            StopChildLocationService();
//            firebaseListener?.Dispose();
//        }

//        private  void OnGetLocationClicked(object sender, EventArgs e)
//        {
//            try
//            {
//                double lat = Preferences.Default.Get("LastLat", 0.0);
//                double lng = Preferences.Default.Get("LastLng", 0.0);
//                string time = Preferences.Default.Get("LastTime", "нет данных");

//                // Преобразуем строку времени из UTC в локальное
//                string localTimeStr = time;
//                try
//                {
//                    if (!string.IsNullOrEmpty(time) && time != "нет данных")
//                    {
//                        DateTime utcTime = DateTime.Parse(time, null, System.Globalization.DateTimeStyles.RoundtripKind);
//                        DateTime localTime = utcTime.ToLocalTime();
//                        localTimeStr = localTime.ToString("yyyy-MM-dd HH:mm:ss");
//                    }
//                }
//                catch
//                {
//                    localTimeStr = time;
//                }

//                ChildLocationLabel.Text = $"Широта: {lat}, Долгота: {lng}\nДата: {localTimeStr}";


//              //  ChildLocationLabel.Text = $"Широта: {lat}, Долгота: {lng}\nДата: {time}";
//            }
//            catch (Exception ex)
//            {
//                ChildLocationLabel.Text = $"Ошибка: {ex.Message}";
//            }
//        }

//        private void OnSatelliteClicked(object sender, EventArgs e)
//        {
//            if (MyMap.MapType == MapType.Street) { MyMap.MapType = MapType.Satellite; }
//            else { MyMap.MapType = MapType.Street; }

//        }

//        //private void OnShowPointsClicked(object sender, EventArgs e)
//        //{
//        //    MainThread.BeginInvokeOnMainThread(() =>
//        //    {
//        //        MyMap.Pins.Clear();
//        //        int i = 1;
//        //        foreach (var pos in childLocations)
//        //        {
//        //            MyMap.Pins.Add(new Pin
//        //            {
//        //                Label = $"Точка {i}",
//        //                Location = pos,
//        //                Type = PinType.SavedPin
//        //            });
//        //            i++;
//        //        }
//        //        if (childLocations.Count > 0)
//        //        {
//        //            // Центрируем карту на последней точке
//        //            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(childLocations.Last(), Distance.FromMeters(200)));
//        //        }
//        //    });
//        //}

//        private void OnShowPointsClicked(object sender, EventArgs e)
//        {
//            MainThread.BeginInvokeOnMainThread(() =>
//            {
//                if (!isTrailShown)
//                {
//                    // Показать след
//                    MyMap.Pins.Clear();
//                    int i = 1;
//                    foreach (var pos in childLocations)
//                    {
//                        MyMap.Pins.Add(new Pin
//                        {
//                            Label = $"Точка {i}",
//                            Location = pos,
//                            Type = PinType.SavedPin
//                        });
//                        i++;
//                    }
//                    if (childLocations.Count > 0)
//                    {
//                        MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(childLocations.Last(), Distance.FromMeters(200)));
//                    }
//                    isTrailShown = true;
//                    ((Button)sender).Text = "Скрыть след";
//                }
//                else
//                {
//                    // Стереть след, показать только последнюю точку
//                    MyMap.Pins.Clear();
//                    if (childLocations.Count > 0)
//                    {
//                        MyMap.Pins.Add(new Pin
//                        {
//                            Label = $"Ребёнок",
//                            Location = childLocations.Last(),
//                            Type = PinType.Place,
//                        });
//                    }
//                    isTrailShown = false;
//                    ((Button)sender).Text = "След";
//                }
//            });
//        }

//    }
//}

























//AIzaSyCXXoJ-HqFBq-J9lvZI5bPNlHxFfvpVJjM - это ключ API для Google Maps, который нужно заменить на свой собственный ключ.
//private async void OnGetLocationClicked(object sender, EventArgs e)
//{
//    try
//    {
//        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
//        if (status != PermissionStatus.Granted)
//        {
//            await DisplayAlert("Ошибка", "Нет разрешения на доступ к геолокации", "OK");
//            return;
//        }

//        var location = await Geolocation.GetLastKnownLocationAsync();
//        if (location == null)
//            location = await Geolocation.GetLocationAsync();

//        if (location != null)
//        {
//            LocationLabel.Text = $"Широта: {location.Latitude}\nДолгота: {location.Longitude}";
//        }
//        else
//        {
//            LocationLabel.Text = "Координаты не определены.";
//        }
//    }
//    catch (Exception ex)
//    {
//        await DisplayAlert("Ошибка", ex.Message, "OK");
//    }
//}



//private async void OnGetLocationClicked(object sender, EventArgs e)
//{
//    try
//    {
//        //Permissions.RequestAsync — обращается к системным настройкам и спрашивает у пользователя
//        //разрешение на получение местоположения.
//        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
//        if (status != PermissionStatus.Granted)
//        {
//            await DisplayAlert("Ошибка", "Нет разрешения на доступ к геолокации", "OK");
//            return;
//        }
//        //Geolocation.GetLastKnownLocationAsync() — пытается получить последние известные координаты 
//        var location = await Geolocation.GetLastKnownLocationAsync();
//        if (location == null)
//            //Если не получилось — делает новый запрос через Geolocation.GetLocationAsync(),
//            //который определяет координаты с помощью GPS/сетей.
//            location = await Geolocation.GetLocationAsync();

//        if (location != null)
//        {
//            LocationLabel.Text = $"Широта: {location.Latitude}\nДолгота: {location.Longitude}";
//            var position = new Location(location.Latitude, location.Longitude);
//            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200)));

//            var pin = new Pin
//            {
//                Label = "Моё местоположение",
//                Location = position,
//                Type = PinType.Place,
//            };
//            MyMap.Pins.Clear();
//            MyMap.Pins.Add(pin);

//            // --- Сохраняем координаты в Firebase ---
//            await SendLocationToFirebase(location.Latitude, location.Longitude);
//        }
//        else
//        {
//            LocationLabel.Text = "Координаты не определены.";
//        }
//    }
//    catch (Exception ex)
//    {
//        await DisplayAlert("Ошибка", ex.Message, "OK");
//    }
//}





//private async Task SendLocationToFirebase(double latitude, double longitude)
//{
//    var firebase = new FirebaseClient(firebaseUrl);
//    await firebase
//        .Child("locations")
//        .PostAsync(new
//        {
//            lat = latitude,
//            lng = longitude,
//            time = DateTime.UtcNow.ToString("o")
//        });
//}

//private async void OnGetLocationClicked(object sender, EventArgs e)
//{
//    try
//    {
//        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
//        if (status != PermissionStatus.Granted)
//        {
//            await DisplayAlert("Ошибка", "Нет разрешения на доступ к геолокации", "OK");
//            return;
//        }

//        var location = await Geolocation.GetLastKnownLocationAsync();
//        if (location == null)
//            location = await Geolocation.GetLocationAsync();

//        if (location != null)
//        {
//            LocationLabel.Text = $"Широта: {location.Latitude}\nДолгота: {location.Longitude}";
//            // Центрируем карту и ставим пин
//            var position = new Location(location.Latitude, location.Longitude);
//            MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200)));

//            var pin = new Pin
//            {
//                Label = "Моё местоположение",
//                Location = position,
//                Type = PinType.Place,
//            };
//            MyMap.Pins.Clear();
//            MyMap.Pins.Add(pin);
//        }
//        else
//        {
//            LocationLabel.Text = "Координаты не определены.";
//        }
//    }
//    catch (Exception ex)
//    {
//        await DisplayAlert("Ошибка", ex.Message, "OK");
//    }
//}




//// Import the functions you need from the SDKs you need
//import { initializeApp } from "firebase/app";
//import { getAnalytics } from "firebase/analytics";
//// TODO: Add SDKs for Firebase products that you want to use
//// https://firebase.google.com/docs/web/setup#available-libraries

//// Your web app's Firebase configuration
//// For Firebase JS SDK v7.20.0 and later, measurementId is optional
//const firebaseConfig = {
//  apiKey: "AIzaSyC0c8YE0hiprN2emwRVq-QvE2hvZ_K--58",
//  authDomain: "gpsdemo-5820b.firebaseapp.com",
//  databaseURL: "https://gpsdemo-5820b-default-rtdb.firebaseio.com",
//  projectId: "gpsdemo-5820b",
//  storageBucket: "gpsdemo-5820b.firebasestorage.app",
//  messagingSenderId: "1086587655298",
//  appId: "1:1086587655298:web:f5ebff0076be3bf19b909b",
//  measurementId: "G-PDNRPCNZB5"
//};

//// Initialize Firebase
//const app = initializeApp(firebaseConfig);
//const analytics = getAnalytics(app);



