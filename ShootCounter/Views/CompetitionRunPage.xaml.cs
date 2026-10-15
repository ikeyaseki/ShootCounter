namespace ShootCounter.Views;

public partial class CompetitionRunPage : ContentPage
{
    private readonly CompetitionRunViewModel _competitionRunViewModel;

    public CompetitionRunPage(CompetitionRunViewModel competitionRunViewModel)
	{
		InitializeComponent();
		BindingContext = competitionRunViewModel;
        _competitionRunViewModel = competitionRunViewModel;
    }

    private void BulletScoring_ValuationChanged(object? sender, EventArgs e)
    {
        _competitionRunViewModel.ContinueCommand.ChangeCanExecute();
    }
}