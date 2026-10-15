namespace ShootCounter.Views;

public partial class TrainingsPage : ContentPage
{
	public TrainingsPage(TrainingsViewModel shootingsViewModel)
	{
		InitializeComponent();
		BindingContext = shootingsViewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		(BindingContext as TrainingsViewModel)?.OnAppearing();
    }

    private void Legend_Clicked(object? sender, EventArgs e)
    {
        Legend.Show();
    }
}