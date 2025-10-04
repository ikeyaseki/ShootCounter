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
        }
    }
}
