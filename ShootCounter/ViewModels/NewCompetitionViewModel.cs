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
        private CompetitionProperties? _selectedCompetition = null;
        private string _location = string.Empty;
        private string _weapon = string.Empty;
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

        public string Weapon
        {
            get => _weapon;
            set
            {
                SetProperty(ref _weapon, value);
                SaveCommand.ChangeCanExecute();
            }
        }

        public Command SaveCommand { get; }
        public Command CancelCommand { get; }
        public NewCompetitionViewModel()
        {
            Title = "Neuer Wettkampf";
            SaveCommand = new Command(OnSave, ValidateSave);
            CancelCommand = new Command(OnCancel);
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        private bool ValidateSave()
        {
            return !string.IsNullOrWhiteSpace(Location) && SelectedCompetition != null && !string.IsNullOrWhiteSpace(Weapon);
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
                Weapon = Weapon
            };

            await CompetitionDataStore.AddItemAsync(competition);

            await Shell.Current.GoToAsync($"{nameof(CompetitionRunPage)}?{nameof(CompetitionRunViewModel.ShootingId)}={competition.Id.ToString()}", true);
        }
    }
}
