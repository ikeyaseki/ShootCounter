namespace ShootCounter.Views;

public partial class ShootingDetailPage : ContentPage
{
	public ShootingDetailPage(ShootingDetailViewModel shootingDetailViewModel)
	{
		InitializeComponent();
		BindingContext = shootingDetailViewModel;
	}
}