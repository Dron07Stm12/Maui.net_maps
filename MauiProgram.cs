using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;

namespace MauiGpsDemo
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            //Это строка регистрирует страницу MainPage в контейнере зависимостей (Dependency Injection — DI)
            //как синглтон (один экземпляр на всё приложение).
            builder.Services.AddSingleton<MainPage>();  

            builder.UseMauiMaps(); // Регистрация карт в приложении 

            return builder.Build();
        }
    }
}
