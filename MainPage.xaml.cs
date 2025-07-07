                                                                                                                                                                                                                                        using Microsoft.Maui;                  // Базовые типы
using Microsoft.Maui.ApplicationModel; // Для PermissionStatus
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;  // Для Geolocation
using Microsoft.Maui.Controls.Maps;    // Для карт  
using Microsoft.Maui.Maps;

using Firebase.Database;
using Firebase.Database.Query;

namespace MauiGpsDemo
{
    public partial class MainPage : ContentPage
    {

        private const string firebaseUrl = "https://gpsdemo-5820b-default-rtdb.firebaseio.com/";
        public MainPage()
        {
            InitializeComponent();
        }

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



        private async void OnGetLocationClicked(object sender, EventArgs e)
        {
            try
            {
                //Permissions.RequestAsync — обращается к системным настройкам и спрашивает у пользователя
                //разрешение на получение местоположения.
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Ошибка", "Нет разрешения на доступ к геолокации", "OK");
                    return;
                }
                //Geolocation.GetLastKnownLocationAsync() — пытается получить последние известные координаты 
                var location = await Geolocation.GetLastKnownLocationAsync();
                if (location == null)
                    //Если не получилось — делает новый запрос через Geolocation.GetLocationAsync(),
                    //который определяет координаты с помощью GPS/сетей.
                    location = await Geolocation.GetLocationAsync();

                if (location != null)
                {
                    LocationLabel.Text = $"Широта: {location.Latitude}\nДолгота: {location.Longitude}";
                    var position = new Location(location.Latitude, location.Longitude);
                    MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromMeters(200)));

                    var pin = new Pin
                    {
                        Label = "Моё местоположение",
                        Location = position,
                        Type = PinType.Place,
                    };
                    MyMap.Pins.Clear();
                    MyMap.Pins.Add(pin);

                    // --- Сохраняем координаты в Firebase ---
                    await SendLocationToFirebase(location.Latitude, location.Longitude);
                }
                else
                {
                    LocationLabel.Text = "Координаты не определены.";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }





        private async Task SendLocationToFirebase(double latitude, double longitude)
        {
            var firebase = new FirebaseClient(firebaseUrl);
            await firebase
                .Child("locations")
                .PostAsync(new
                {
                    lat = latitude,
                    lng = longitude,
                    time = DateTime.UtcNow.ToString("o")
                });
        }

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


    }
}
