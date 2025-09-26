namespace ShootCounter
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ShootingDetailPage), typeof(ShootingDetailPage));
            Routing.RegisterRoute(nameof(NewShootingPage), typeof(NewShootingPage));
            Routing.RegisterRoute(nameof(RunPage), typeof(RunPage));
            Routing.RegisterRoute(nameof(ShootingsPage), typeof(ShootingsPage));
        }
    }
}
