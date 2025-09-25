namespace ShootCounter.Views;

public partial class NewItemPage : ContentPage
{
	public NewItemPage(NewItemViewModel newItemViewModel)
	{
		InitializeComponent();
		BindingContext = newItemViewModel;
	}
}