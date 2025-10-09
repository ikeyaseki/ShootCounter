namespace ShootCounter.Views;

public partial class TrainingDetailPage : ContentPage
{
	public TrainingDetailPage(TrainingDetailViewModel shootingDetailViewModel)
	{
		InitializeComponent();
		BindingContext = shootingDetailViewModel;
	}
}