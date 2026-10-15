using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    // creates a new weapon, or edits an existing one when WeaponId is passed
    [QueryProperty(nameof(WeaponId), nameof(WeaponId))]
    public class WeaponDetailViewModel : BaseViewModel
    {
        private readonly IDataStore<Weapon> _weaponDataStore;
        private Weapon _weapon = new();
        private string _weaponId = string.Empty;
        private string _manufacturer = string.Empty;
        private string _model = string.Empty;
        private string _caliber = string.Empty;
        private bool _hasPurchaseDate;
        private DateTime _purchaseDate = DateTime.Today;
        private string _notes = string.Empty;
        private bool _isExisting;
        private bool _isSaving;

        public Command SaveCommand { get; }
        public Command DeleteCommand { get; }

        public string WeaponId
        {
            get => _weaponId;
            set
            {
                _weaponId = value;
                LoadWeaponAsync(value);
            }
        }

        public string Manufacturer
        {
            get => _manufacturer;
            set
            {
                SetProperty(ref _manufacturer, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public string Model
        {
            get => _model;
            set
            {
                SetProperty(ref _model, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public string Caliber
        {
            get => _caliber;
            set => SetProperty(ref _caliber, value);
        }

        public bool HasPurchaseDate
        {
            get => _hasPurchaseDate;
            set => SetProperty(ref _hasPurchaseDate, value);
        }

        public DateTime PurchaseDate
        {
            get => _purchaseDate;
            set => SetProperty(ref _purchaseDate, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public bool IsExisting
        {
            get => _isExisting;
            set
            {
                SetProperty(ref _isExisting, value);
                DeleteCommand.ChangeCanExecute();
            }
        }

        public WeaponDetailViewModel(IDataStore<Weapon> weaponDataStore)
        {
            _weaponDataStore = weaponDataStore;
            Title = "Neue Waffe";
            SaveCommand = new Command(OnSave, ValidateSave);
            DeleteCommand = new Command(OnDelete, () => IsExisting);
        }

        private async void LoadWeaponAsync(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var weaponId))
                    return;

                var weapon = await _weaponDataStore.GetItemAsync(weaponId);
                if (weapon == null)
                    return;

                _weapon = weapon;
                Manufacturer = weapon.Manufacturer ?? string.Empty;
                Model = weapon.Model ?? string.Empty;
                Caliber = weapon.Caliber ?? string.Empty;
                HasPurchaseDate = weapon.PurchaseDate.HasValue;
                PurchaseDate = weapon.PurchaseDate ?? DateTime.Today;
                Notes = weapon.Notes ?? string.Empty;
                IsExisting = true;
                Title = "Waffe bearbeiten";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private bool ValidateSave()
        {
            return !string.IsNullOrWhiteSpace(Manufacturer) && !string.IsNullOrWhiteSpace(Model);
        }

        private async void OnSave()
        {
            // a fast double tap would otherwise save twice
            if (_isSaving)
                return;

            _isSaving = true;
            try
            {
                _weapon.Manufacturer = Manufacturer.Trim();
                _weapon.Model = Model.Trim();
                _weapon.Caliber = string.IsNullOrWhiteSpace(Caliber) ? null : Caliber.Trim();
                _weapon.PurchaseDate = HasPurchaseDate ? PurchaseDate.Date : null;
                _weapon.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

                await _weaponDataStore.UpdateItemAsync(_weapon);

                await Shell.Current.GoToAsync("..", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Die Waffe konnte nicht gespeichert werden.", "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async void OnDelete()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Waffe löschen",
                "Willst du die Waffe wirklich löschen? Bestehende Trainings und Wettkämpfe behalten den Waffennamen.",
                "Ja",
                "Nein");

            if (!confirm)
                return;

            try
            {
                await _weaponDataStore.DeleteItemAsync(_weapon.Id);

                await Shell.Current.GoToAsync("..", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Die Waffe konnte nicht gelöscht werden.", "OK");
            }
        }
    }
}
