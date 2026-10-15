using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace ShootCounter
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseSkiaSharp()
                .UseLiveCharts()
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
            builder.Services.AddSingleton<IDataStore<Shooting>, TrainingDataStore>();
            builder.Services.AddSingleton<IDataStore<ShootingCompetition>, CompetitionDataStore>();

            // register our pages
            builder.Services.AddSingleton<TrainingsPage>();
            builder.Services.AddTransient<TrainingDetailPage>();
            builder.Services.AddTransient<NewTrainingPage>();
            builder.Services.AddTransient<RunPage>();
            builder.Services.AddSingleton<CompetitionsPage>();
            builder.Services.AddTransient<CompetitionDetailPage>();
            builder.Services.AddTransient<NewCompetitionPage>();
            builder.Services.AddTransient<CompetitionRunPage>();

            // register viewmodels
            builder.Services.AddSingleton<AboutViewModel>();
            builder.Services.AddSingleton<TrainingsViewModel>();
            builder.Services.AddTransient<TrainingDetailViewModel>();
            builder.Services.AddTransient<NewTrainingViewModel>();
            builder.Services.AddTransient<RunViewModel>();
            builder.Services.AddSingleton<CompetitionsViewModel>();
            builder.Services.AddTransient<NewCompetitionViewModel>();
            builder.Services.AddTransient<CompetitionDetailViewModel>();
            builder.Services.AddTransient<CompetitionRunViewModel>();

            var app = builder.Build();

            return app;
        }
    }
}
