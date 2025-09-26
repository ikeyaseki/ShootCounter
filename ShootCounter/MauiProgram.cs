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
                })
                .ConfigureEssentials(essentials =>
                {
                    essentials.UseVersionTracking();
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // register our service
            builder.Services.AddSingleton<IDataStore<Item>, MockDataStore>();
            builder.Services.AddSingleton<IItemStore<Shooting>, ShootingDataStore>();

            // register our pages
            builder.Services.AddSingleton<ItemsPage>();
            builder.Services.AddTransient<NewItemPage>();
            builder.Services.AddTransient<ItemDetailPage>();

            builder.Services.AddSingleton<ShootingsPage>();
            builder.Services.AddTransient<ShootingDetailPage>();

            // register viewmodels
            builder.Services.AddSingleton<AboutViewModel>();
            builder.Services.AddSingleton<ItemsViewModel>();
            builder.Services.AddTransient<NewItemViewModel>();
            builder.Services.AddTransient<ItemDetailViewModel>();

            builder.Services.AddSingleton<ShootingsViewModel>();
            builder.Services.AddTransient<ShootingDetailViewModel>();

            var app = builder.Build();

            // initalize our service helper before calling it
            ServiceHelper.Initialize(app.Services);

            return app;
        }
    }
}
