namespace ShootCounter.Views;

public partial class RunPage : ContentPage
{
    private readonly RunViewModel _runViewModel;

    public RunPage(RunViewModel runViewModel)
	{
		InitializeComponent();
		BindingContext = runViewModel;
        _runViewModel = runViewModel;
    }

    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _runViewModel.SaveCommand.ChangeCanExecute();
        _runViewModel.StopCommand.ChangeCanExecute();
    }
}