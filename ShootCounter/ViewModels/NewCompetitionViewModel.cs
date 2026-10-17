using System.Collections.ObjectModel;

using ShootCounter.Converter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    public class NewCompetitionViewModel : BaseViewModel
    {
        private readonly IDataStore<ShootingCompetition> _competitionDataStore;
        private CompetitionProperties? _selectedCompetition = null;
        private string _location = string.Empty;
        private readonly IDataStore<Weapon> _weaponDataStore;
        private Weapon? _selectedWeapon;
        private bool _hasNoWeapons;
        private bool _isSaving;

        public IEnumerable<CompetitionProperties> Competitions { get; } = Models.Competitions.CompetitionProperties;

        public CompetitionProperties? SelectedCompetition 
        { 
            get => _selectedCompetition; 
            set
            {
                SetProperty(ref _selectedCompetition, value);
                SaveCommand.ChangeCanExecute();
            }
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

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public NewCompetitionViewModel(IDataStore<ShootingCompetition> competitionDataStore, IDataStore<Weapon> weaponDataStore)
        {
            _weaponDataStore = weaponDataStore;
            _competitionDataStore = competitionDataStore;
            Title = "Neuer Wettkampf";
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
            Location = LastLocation.Value;
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
            return !string.IsNullOrWhiteSpace(Location) && SelectedCompetition != null && SelectedWeapon != null;
        }

        private async void OnSave()
        {
            // a fast double tap would otherwise create two competitions
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
                await Shell.Current.DisplayAlert("Fehler", "Der Wettkampf konnte nicht gespeichert werden.", "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task SaveAsync()
        {
            ShootingCompetition competition = new()
            {
                StartTime = DateTime.Now,
                Location = Location,
                CompetitionType = SelectedCompetition?.CompetitionType ?? CompetitionType.Unknown,
                Weapon = SelectedWeapon?.DisplayName,
                WeaponId = SelectedWeapon?.Id
            };

            await _competitionDataStore.AddItemAsync(competition);
            LastLocation.Value = Location;

            await Shell.Current.GoToAsync($"{nameof(CompetitionRunPage)}?{nameof(CompetitionRunViewModel.ShootingId)}={competition.Id.ToString()}", true);
        }
    }
}
