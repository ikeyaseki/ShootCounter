using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    // creates a new weapon, or edits an existing one when WeaponId is passed
    [QueryProperty(nameof(WeaponId), nameof(WeaponId))]
    public class WeaponDetailViewModel : BaseViewModel
    {
        private readonly IDataStore<Weapon> _weaponDataStore;
        private readonly IDatabaseService _db;
        private Weapon _weapon = new();
        private string _weaponId = string.Empty;
        private string _manufacturer = string.Empty;
        private string? _selectedManufacturer;
        private string _model = string.Empty;
        private string _caliber = Calibers.None;
        private IReadOnlyList<string> _caliberOptions = [Calibers.None, .. Calibers.All];
        private bool _hasPurchaseDate;
        private DateTime _purchaseDate = DateTime.Today;
        private string _notes = string.Empty;
        private bool _isExisting;
        private bool _isSaving;
        private string _retireText = "Ausmustern";
        private bool _canDelete;

        public Command SaveCommand { get; }
        public Command RetireCommand { get; }
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

        public IReadOnlyList<string> ManufacturerOptions { get; } = [.. Manufacturers.All, Manufacturers.Other];

        public string? SelectedManufacturer
        {
            get => _selectedManufacturer;
            set
            {
                SetProperty(ref _selectedManufacturer, value);
                OnPropertyChanged(nameof(IsOtherManufacturer));
                SaveCommand.ChangeCanExecute();
            }
        }

        public bool IsOtherManufacturer => SelectedManufacturer == Manufacturers.Other;

        // the name typed by hand when the manufacturer is not in the list
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

        // the picker clears the selection while its items are replaced
        public string Caliber
        {
            get => _caliber;
            set
            {
                if (value != null)
                    SetProperty(ref _caliber, value);
            }
        }

        public IReadOnlyList<string> CaliberOptions
        {
            get => _caliberOptions;
            set => SetProperty(ref _caliberOptions, value);
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
                RetireCommand.ChangeCanExecute();
            }
        }

        // only a retired weapon can be deleted, together with all its data
        public bool CanDelete
        {
            get => _canDelete;
            set => SetProperty(ref _canDelete, value);
        }

        public string RetireText
        {
            get => _retireText;
            set => SetProperty(ref _retireText, value);
        }

        public WeaponDetailViewModel(IDataStore<Weapon> weaponDataStore, IDatabaseService db)
        {
            _db = db;
            _weaponDataStore = weaponDataStore;
            Title = "Neue Waffe";
            SaveCommand = new Command(OnSave, ValidateSave);
            RetireCommand = new Command(OnToggleRetired, () => IsExisting);
            DeleteCommand = new Command(OnDelete);
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
                SelectManufacturer(weapon.Manufacturer);
                Model = weapon.Model ?? string.Empty;
                SelectCaliber(weapon.Caliber);
                HasPurchaseDate = weapon.PurchaseDate.HasValue;
                PurchaseDate = weapon.PurchaseDate ?? DateTime.Today;
                Notes = weapon.Notes ?? string.Empty;
                IsExisting = true;
                RetireText = weapon.IsRetired ? "Reaktivieren" : "Ausmustern";
                CanDelete = weapon.IsRetired;
                Title = "Waffe bearbeiten";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private string? ManufacturerName => IsOtherManufacturer ? Manufacturer.Trim() : SelectedManufacturer;

        // a manufacturer typed by hand before the list existed is matched to the list,
        // otherwise it is kept as "other manufacturer"
        private void SelectManufacturer(string? manufacturer)
        {
            var key = StatisticsService.NormalizeName(manufacturer);
            var match = Manufacturers.All.FirstOrDefault(n => StatisticsService.NormalizeName(n) == key);

            if (match != null)
            {
                SelectedManufacturer = match;
            }
            else if (!string.IsNullOrWhiteSpace(manufacturer))
            {
                SelectedManufacturer = Manufacturers.Other;
                Manufacturer = manufacturer.Trim();
            }
        }

        // calibers typed by hand before the list existed are matched to the list,
        // an unknown one stays selectable so it is not lost when saving
        private void SelectCaliber(string? caliber)
        {
            if (string.IsNullOrWhiteSpace(caliber))
            {
                Caliber = Calibers.None;
                return;
            }

            var key = StatisticsService.NormalizeName(caliber);
            var match = Calibers.All.FirstOrDefault(n => StatisticsService.NormalizeName(n) == key);

            if (match == null)
            {
                match = caliber.Trim();
                CaliberOptions = [Calibers.None, match, .. Calibers.All];
            }

            Caliber = match;
        }

        private bool ValidateSave()
        {
            return !string.IsNullOrWhiteSpace(ManufacturerName) && !string.IsNullOrWhiteSpace(Model);
        }

        private async void OnSave()
        {
            // a fast double tap would otherwise save twice
            if (_isSaving)
                return;

            _isSaving = true;
            try
            {
                _weapon.Manufacturer = ManufacturerName;
                _weapon.Model = Model.Trim();
                _weapon.Caliber = Caliber == Calibers.None ? null : Caliber;
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

        // a retired weapon is not offered for new trainings, but stays in the statistics
        private async void OnToggleRetired()
        {
            bool retire = !_weapon.IsRetired;

            bool confirm = await Shell.Current.DisplayAlert(
                retire ? "Waffe ausmustern" : "Waffe reaktivieren",
                retire
                    ? "Die Waffe wird bei neuen Trainings und Wettkämpfen nicht mehr angeboten. In der Statistik bleibt sie erhalten."
                    : "Die Waffe wird bei neuen Trainings und Wettkämpfen wieder angeboten.",
                "Ja",
                "Nein");

            if (!confirm)
                return;

            try
            {
                _weapon.IsRetired = retire;
                await _weaponDataStore.UpdateItemAsync(_weapon);

                await Shell.Current.GoToAsync("..", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Die Waffe konnte nicht gespeichert werden.", "OK");
            }
        }

        // asks twice, deleting removes the weapon with all its trainings and competitions for good
        private async void OnDelete()
        {
            try
            {
                var (trainings, competitions) = await _db.CountWeaponUsageAsync(_weapon.Id);

                string message = trainings + competitions == 0
                    ? "Mit dieser Waffe sind keine Trainings oder Wettkämpfe verknüpft."
                    : $"Mit der Waffe werden auch {Count(trainings, "Training", "Trainings")} und {Count(competitions, "Wettkampf", "Wettkämpfe")} gelöscht. Sie fehlen danach auch in der Statistik.";

                bool confirm = await Shell.Current.DisplayAlert(
                    "Waffe endgültig löschen",
                    message,
                    "Weiter",
                    "Abbrechen");

                if (!confirm)
                    return;

                confirm = await Shell.Current.DisplayAlert(
                    "Wirklich löschen?",
                    $"\"{_weapon.DisplayName}\" und alle zugehörigen Daten werden gelöscht. Das kann nicht rückgängig gemacht werden.",
                    "Endgültig löschen",
                    "Abbrechen");

                if (!confirm)
                    return;

                await _db.DeleteWeaponAsync(_weapon.Id);

                await Shell.Current.GoToAsync("..", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Die Waffe konnte nicht gelöscht werden.", "OK");
            }
        }

        private static string Count(int count, string singular, string plural) =>
            $"{count} {(count == 1 ? singular : plural)}";
    }
}
