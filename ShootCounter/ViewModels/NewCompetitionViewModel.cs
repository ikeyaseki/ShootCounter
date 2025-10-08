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

            Shell.Current.Navigating += (s, e) =>
            {
                if (e.Source == ShellNavigationSource.Pop && e.CanCancel)
                {
                    e.Cancel();
                }
            };
        }

        private async void OnCancel()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        private bool ValidateSave()
        {
            return Location != string.Empty && SelectedCompetition != null && Weapon != string.Empty;
        }

        private async void OnSave()
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
