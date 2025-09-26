namespace ShootCounter.Views;

public partial class NewShootingPage : ContentPage
{
	public NewShootingPage(NewShootingViewModel newShootingViewModel)
	{
		InitializeComponent();
		BindingContext = newShootingViewModel;
	}
}