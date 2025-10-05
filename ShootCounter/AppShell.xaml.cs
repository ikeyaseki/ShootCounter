namespace ShootCounter
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(TrainingDetailPage), typeof(TrainingDetailPage));
            Routing.RegisterRoute(nameof(NewTrainingPage), typeof(NewTrainingPage));
            Routing.RegisterRoute(nameof(RunPage), typeof(RunPage));
            Routing.RegisterRoute(nameof(TrainingsPage), typeof(TrainingsPage));
            Routing.RegisterRoute(nameof(CompetitionsPage), typeof(CompetitionsPage));
            Routing.RegisterRoute(nameof(NewCompetitionPage), typeof(NewCompetitionPage));
            Routing.RegisterRoute(nameof(CompetitionDetailPage), typeof(CompetitionDetailPage));
            Routing.RegisterRoute(nameof(CompetitionRunPage), typeof(CompetitionRunPage));
        }
    }
}
