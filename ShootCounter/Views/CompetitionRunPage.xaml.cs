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

    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _competitionRunViewModel.ContinueCommand.ChangeCanExecute();
    }
}