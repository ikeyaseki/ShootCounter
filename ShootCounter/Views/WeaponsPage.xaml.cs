namespace ShootCounter.Views;

public partial class WeaponsPage : ContentPage
{
    public WeaponsPage(WeaponsViewModel weaponsViewModel)
    {
        InitializeComponent();
        BindingContext = weaponsViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as WeaponsViewModel)?.OnAppearing();
    }
}
