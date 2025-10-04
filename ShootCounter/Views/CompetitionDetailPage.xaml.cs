namespace ShootCounter.Views;

public partial class CompetitionDetailPage : ContentPage
{
	public CompetitionDetailPage(CompetitionDetailViewModel competitionDetailViewModel)
	{
		InitializeComponent();
		BindingContext = competitionDetailViewModel;
	}
}