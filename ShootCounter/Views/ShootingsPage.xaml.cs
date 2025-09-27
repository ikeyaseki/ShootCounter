namespace ShootCounter.Views;

public partial class ShootingsPage : ContentPage
{
	public ShootingsPage(ShootingsViewModel shootingsViewModel)
	{
		InitializeComponent();
		BindingContext = shootingsViewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		(BindingContext as ShootingsViewModel)?.OnAppearing();
    }
}