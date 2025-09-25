namespace ShootCounter.Views;

public partial class ItemsPage : ContentPage
{
	public ItemsPage(ItemsViewModel itemsViewModel)
	{
		InitializeComponent();
		BindingContext = itemsViewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
		(BindingContext as ItemsViewModel)?.OnAppearing();
    }
}