namespace ShootCounter.Views;

public partial class WeaponDetailPage : ContentPage
{
    public WeaponDetailPage(WeaponDetailViewModel weaponDetailViewModel)
    {
        InitializeComponent();
        BindingContext = weaponDetailViewModel;
    }
}
