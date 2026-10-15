using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    public class WeaponsViewModel : BaseViewModel
    {
        private readonly IDataStore<Weapon> _weaponDataStore;

        public ObservableCollection<Weapon> Weapons { get; } = [];
        public Command LoadWeaponsCommand { get; }
        public Command AddWeaponCommand { get; }
        public Command<Weapon> WeaponTapped { get; }

        public WeaponsViewModel(IDataStore<Weapon> weaponDataStore)
        {
            _weaponDataStore = weaponDataStore;
            Title = "Waffen";
            LoadWeaponsCommand = new Command(async () => await ExecuteLoadWeaponsCommand());
            AddWeaponCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(WeaponDetailPage), true));
            WeaponTapped = new Command<Weapon>(OnWeaponTapped);
        }

        private async Task ExecuteLoadWeaponsCommand()
        {
            IsBusy = true;

            try
            {
                var weapons = await _weaponDataStore.GetItemsAsync(true);

                Weapons.Clear();
                // retired weapons at the end of the list
                foreach (var weapon in weapons.OrderBy(n => n.IsRetired))
                    Weapons.Add(weapon);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnWeaponTapped(Weapon? weapon)
        {
            if (weapon == null)
                return;

            await Shell.Current.GoToAsync($"{nameof(WeaponDetailPage)}?{nameof(WeaponDetailViewModel.WeaponId)}={weapon.Id}", true);
        }

        public void OnAppearing()
        {
            IsBusy = true;
        }
    }
}
