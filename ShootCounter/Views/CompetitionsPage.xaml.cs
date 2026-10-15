namespace ShootCounter.Views;

public partial class CompetitionsPage : ContentPage
{
	public CompetitionsPage(CompetitionsViewModel competitionsViewModel)
	{
		InitializeComponent();
		BindingContext = competitionsViewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as CompetitionsViewModel)?.OnAppearing();
    }

    private void Legend_Clicked(object? sender, EventArgs e)
    {
        Legend.Show();
    }
}