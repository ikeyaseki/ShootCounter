namespace ShootCounter.Views;

public partial class NewTrainingPage : ContentPage
{
	public NewTrainingPage(NewTrainingViewModel newShootingViewModel)
	{
		InitializeComponent();
		BindingContext = newShootingViewModel;
	}
}