using Microsoft.Extensions.Logging;

namespace ShootCounter
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
                    fonts.AddFont("fontello.ttf", "IconFont");
                })
                .ConfigureEssentials(essentials =>
                {
                    essentials.UseVersionTracking();
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // register our service
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IDataStore<Shooting>, ShootingDataStore>();

            // register our pages
            builder.Services.AddSingleton<ShootingsPage>();
            builder.Services.AddTransient<ShootingDetailPage>();
            builder.Services.AddTransient<NewShootingPage>();
            builder.Services.AddTransient<RunPage>();

            // register viewmodels
            builder.Services.AddSingleton<AboutViewModel>();
            builder.Services.AddSingleton<ShootingsViewModel>();
            builder.Services.AddTransient<ShootingDetailViewModel>();
            builder.Services.AddTransient<NewShootingViewModel>();
            builder.Services.AddTransient<RunViewModel>();

            var app = builder.Build();

            // initalize our service helper before calling it
            ServiceHelper.Initialize(app.Services);

            return app;
        }
    }
}
