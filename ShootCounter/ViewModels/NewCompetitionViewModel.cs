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
        private IEnumerable<string> _competitionInfos;

        public IEnumerable<CompetitionProperties> Competitions { get; } = Models.Competitions.CompetitionProperties;

        public CompetitionProperties? SelectedCompetition 
        { 
            get => _selectedCompetition; 
            set
            {
                SetProperty(ref _selectedCompetition, value);
                SaveCommand.ChangeCanExecute();
                GetCompetitionInfos();
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

        public IEnumerable<string> CompetitionInfos 
        { 
            get => _competitionInfos; 
            set
            {
                SetProperty(ref _competitionInfos, value);
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
                    if (Shell.Current.CurrentPage is RunPage)
                    {
                        e.Cancel();
                    }
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

        private void GetCompetitionInfos()
        {
            var competitionInfos = new List<string>();

            if (SelectedCompetition?.CompetitionRuns == null)
                throw new Exception("CompetitionRuns not allowed to be null here");

            var target = Targets.TargetProperties.First(n => n.TargetType == SelectedCompetition.TargetType);

            DistanceTypeToStringConverter converter = new DistanceTypeToStringConverter();
            competitionInfos.Add($"Distanz: {converter.Convert(SelectedCompetition.DistanceType, typeof(DistanceType), null, new CultureInfo("de-DE"))}");
            competitionInfos.Add($"Scheibe: {target.Name}");

            var trials = "Probeschüsse: ";
            trials = SelectedCompetition.TrialShots ? $"{trials} Ja" : $"{trials} Keine";
            competitionInfos.Add(trials);

            foreach (var run in SelectedCompetition.CompetitionRuns)
            {
                var text = $"- {run.NumberOfBullets} Schuss";

                if (run.Einzelgezeigt)
                {
                    if (run.Duration != null)
                    {
                        text = $"{text} in je {run.Duration}s einzeln gezeigt";
                    }
                    else
                    {
                        text = $"{text} einzeln gezeigt";
                    }
                }
                else
                {
                    if (run.Duration != null)
                        text = $"{text} in {run.Duration}s";
                }

                competitionInfos.Add(text);
            }

            competitionInfos.Add($"Maximale Punktzahl: {SelectedCompetition.MaxPoints}");

            if (SelectedCompetition.MinPoints != null)
                competitionInfos.Add($"Mindest Punktzahl: {SelectedCompetition.MinPoints}");

            if (SelectedCompetition.MaxZeros != null)
                competitionInfos.Add($"Maximale Fehlschüsse: {SelectedCompetition.MaxZeros}");

            if (SelectedCompetition.KartePoints != null)
                competitionInfos.Add($"Punktzahl für Karte: {SelectedCompetition.KartePoints}");

            if (SelectedCompetition.KranzPoints != null)
                competitionInfos.Add($"Punktzahl für Kranz: {SelectedCompetition.KranzPoints}");

            CompetitionInfos = competitionInfos;
        }
    }
}
