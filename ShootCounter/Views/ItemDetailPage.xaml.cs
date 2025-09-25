namespace ShootCounter.Views;

public partial class ItemDetailPage : ContentPage
{
	public ItemDetailPage(ItemDetailViewModel itemDetailViewModel)
	{
		InitializeComponent();
		BindingContext = itemDetailViewModel;
	}
}