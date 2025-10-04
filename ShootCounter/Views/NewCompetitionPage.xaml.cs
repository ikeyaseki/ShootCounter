namespace ShootCounter.Views;

public partial class NewCompetitionPage : ContentPage
{
	public NewCompetitionPage(NewCompetitionViewModel newCompetitionViewModel)
	{
		InitializeComponent();
		BindingContext = newCompetitionViewModel;
	}
}