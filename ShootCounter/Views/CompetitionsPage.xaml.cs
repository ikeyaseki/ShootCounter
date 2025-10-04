namespace ShootCounter.Views;

public partial class CompetitionsPage : ContentPage
{
	public CompetitionsPage(CompetitionsViewModel competitionsViewModel)
	{
		InitializeComponent();
		BindingContext = competitionsViewModel;
	}
}