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

            // register our pages
            builder.Services.AddSingleton<ItemsPage>();
            builder.Services.AddTransient<NewItemPage>();
            builder.Services.AddTransient<ItemDetailPage>();

            // register viewmodels
            builder.Services.AddSingleton<AboutViewModel>();
            builder.Services.AddSingleton<ItemsViewModel>();
            builder.Services.AddTransient<NewItemViewModel>();
            builder.Services.AddTransient<ItemDetailViewModel>();

            var app = builder.Build();

            // initalize our service helper before calling it
            ServiceHelper.Initialize(app.Services);

            return app;
        }
    }
}
