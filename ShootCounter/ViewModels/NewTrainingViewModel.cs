using System.Collections.ObjectModel;

namespace ShootCounter.ViewModels
{
    public class NewTrainingViewModel : BaseViewModel
    {
        private readonly IDataStore<Shooting> _trainingDataStore;
        private string _location = string.Empty;
        private TargetProperties? _selectedTarget = null;
        private DistanceType _selectedDistance;
        private List<TargetProperties> _targets = [];
        private int? _bulletsPerRun = null;
        private readonly IDataStore<Weapon> _weaponDataStore;
        private Weapon? _selectedWeapon;
        private bool _hasNoWeapons;
        private bool _isSaving;

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }

        // the targets shot at the selected distance
        public List<TargetProperties> Targets
        {
            get => _targets;
            private set => SetProperty(ref _targets, value);
        }

        public TargetProperties? SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                SetProperty(ref _selectedTarget, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public int? BulletsPerRun
        {
            get => _bulletsPerRun;
            set
            {
                SetProperty(ref _bulletsPerRun, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public List<DistanceType> Distances { get; set; } = new List<DistanceType>() { DistanceType.Distance25m, DistanceType.Distance50m };
        public DistanceType SelectedDistance
        {
            get => _selectedDistance;
            set
            {
                SetProperty(ref _selectedDistance, value);
                UpdateTargets();
            }
        }

        private void UpdateTargets()
        {
            // a target of the other distance can not stay selected, a single one needs no choice
            var targets = Models.Targets.TargetProperties.Where(n => n.Distance == SelectedDistance).ToList();
            if (SelectedTarget == null || !targets.Contains(SelectedTarget))
                SelectedTarget = targets.Count == 1 ? targets[0] : null;

            // a new list instead of changing the old one: the list view then creates new item views,
            // reused ones could keep the selection highlight of the previous distance
            Targets = targets;
        }

        public string Location
        {
            get => _location;
            set
            {
                SetProperty(ref _location, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public ObservableCollection<Weapon> Weapons { get; } = [];

        public Weapon? SelectedWeapon
        {
            get => _selectedWeapon;
            set
            {
                SetProperty(ref _selectedWeapon, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        // shows the hint to create a weapon in the weapons tab first
        public bool HasNoWeapons
        {
            get => _hasNoWeapons;
            set => SetProperty(ref _hasNoWeapons, value);
        }

        public NewTrainingViewModel(IDataStore<Shooting> trainingDataStore, IDataStore<Weapon> weaponDataStore)
        {
            _weaponDataStore = weaponDataStore;
            _trainingDataStore = trainingDataStore;
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            Title = "Neues Training";
            Location = LastLocation.Value;
            UpdateTargets();
            LoadWeaponsAsync();
        }

        private async void LoadWeaponsAsync()
        {
            try
            {
                var weapons = (await _weaponDataStore.GetItemsAsync()).Where(n => !n.IsRetired).ToList();

                foreach (var weapon in weapons)
                    Weapons.Add(weapon);

                // with only one weapon there is nothing to choose
                if (weapons.Count == 1)
                    SelectedWeapon = weapons[0];

                HasNoWeapons = weapons.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        private bool ValidateSave()
        {
            return !string.IsNullOrWhiteSpace(Location) && SelectedTarget != null && BulletsPerRun > 0 && SelectedWeapon != null;
        }

        private async void OnSave()
        {
            // a fast double tap would otherwise create two trainings
            if (_isSaving)
                return;

            _isSaving = true;
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Das Training konnte nicht gespeichert werden.", "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task SaveAsync()
        {
            Shooting shooting = new()
            {
                StartTime = DateTime.Now,
                Location = Location,
                TargetType = SelectedTarget?.TargetType ?? TargetType.Unknown,
                Weapon = SelectedWeapon?.DisplayName,
                WeaponId = SelectedWeapon?.Id,
                DistanceType = SelectedDistance,
                BulletsPerRun = BulletsPerRun
            };

            await _trainingDataStore.AddItemAsync(shooting);
            LastLocation.Value = Location;

            await Shell.Current.GoToAsync($"{nameof(RunPage)}?{nameof(RunViewModel.ShootingId)}={shooting.Id.ToString()}", true);
        }
    }
}
