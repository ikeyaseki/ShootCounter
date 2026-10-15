namespace ShootCounter
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // the tab root pages (AboutPage, TrainingsPage, CompetitionsPage) get their route from AppShell.xaml
            Routing.RegisterRoute(nameof(TrainingDetailPage), typeof(TrainingDetailPage));
            Routing.RegisterRoute(nameof(NewTrainingPage), typeof(NewTrainingPage));
            Routing.RegisterRoute(nameof(RunPage), typeof(RunPage));
            Routing.RegisterRoute(nameof(NewCompetitionPage), typeof(NewCompetitionPage));
            Routing.RegisterRoute(nameof(CompetitionDetailPage), typeof(CompetitionDetailPage));
            Routing.RegisterRoute(nameof(CompetitionRunPage), typeof(CompetitionRunPage));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            // a running training or competition must be ended with its own button,
            // the hardware back button must not leave it by accident
            if (args.Source == ShellNavigationSource.Pop && args.CanCancel
                && CurrentPage is RunPage or CompetitionRunPage)
            {
                args.Cancel();
            }
        }
    }
}
