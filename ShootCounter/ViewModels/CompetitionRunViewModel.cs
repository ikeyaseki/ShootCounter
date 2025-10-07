using ShootCounter.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class CompetitionRunViewModel : BaseViewModel
    {
        private string _shootingId = string.Empty;
        private ShootingCompetition _shooting = new();
        private IEnumerable<BulletTemplate> _bullets = [];
        private int _runNumber;
        private string _passeString = string.Empty;
        private bool _showTargetValuations;
        private CompetitionState _competitionState;
        private int _singleShoot = 0;
        private string _singleShootString = string.Empty;

        public Command SaveCommand { get; }
        public Command StopCommand { get; }
        public Command ContinueCommand { get; }

        public int SingleShoot
        {
            get => _singleShoot;
            set
            {
                SetProperty(ref _singleShoot, value);
                SingleShootString = _singleShoot != 0 ? $"Einzelschuss {_singleShoot}" : string.Empty;
            }
        }

        public string SingleShootString
        {
            get => _singleShootString;
            set
            {
                SetProperty(ref _singleShootString, value);
            }
        }

        public CompetitionState CompetitionState
        {
            get => _competitionState;
            set
            {
                SetProperty(ref _competitionState, value);
            }
        }

        public bool ShowTargetValuations
        {
            get => _showTargetValuations;
            set
            {
                SetProperty(ref _showTargetValuations, value);
            }
        }

        public string PasseString
        {
            get => _passeString;
            set
            {
                SetProperty(ref _passeString, value);
            }
        }

        public ShootingCompetition Shooting
        {
            get => _shooting;
            set
            {
                SetProperty(ref _shooting, value);
            }
        }

        public string ShootingId
        {
            get => _shootingId;
            set
            {
                _shootingId = value;
                LoadItemId(value);
            }
        }

        public int RunNumber
        {
            get => _runNumber;
            set
            {
                SetProperty(ref _runNumber, value);
                PasseString = _runNumber != 0 ? $"Passe {_runNumber}" : string.Empty;
            }
        }

        public IEnumerable<BulletTemplate> Bullets
        {
            get => _bullets;
            set
            {
                SetProperty(ref _bullets, value);
            }
        }

        public CompetitionRunViewModel()
        {
            Title = "Wettkampf";
            CompetitionState = CompetitionState.Info;
            SaveCommand = new Command(OnSave, CanExecuteSave);
            StopCommand = new Command(OnStop, CanExecuteStop);
            ContinueCommand = new Command(OnContinue, CanExecuteContinue);

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

        private bool CanExecuteContinue()
        {
            return true;
        }

        private void OnContinue()
        {
           
            // ToDo: save valuations
              

            StateChange();
        }

        private void StateChange()
        {
            switch (CompetitionState)
            {
                case CompetitionState.Info:
                    RunNumber = 1;

                    if (Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt)
                        SingleShoot = 1;

                    CompetitionState = CompetitionState.Prepare;
                    break;
                case CompetitionState.Prepare:
                    CompetitionState = CompetitionState.Shooting;
                    break;
                case CompetitionState.Shooting:
                    CompetitionState = CompetitionState.CountPoints;
                    break;
                case CompetitionState.CountPoints:
                    if (Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt)
                    {
                        if (SingleShoot >= Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).NumberOfBullets)
                        {
                            if (RunNumber >= Shooting.CompetitionProperties.CompetitionRuns.Count())
                            {
                                SingleShoot = 0;
                                RunNumber = 0;
                                CompetitionState = CompetitionState.Result;
                            }
                            else
                            {
                                SingleShoot = 0;
                                RunNumber++;
                                CompetitionState = CompetitionState.Prepare;
                            }
                        }
                        else
                        {
                            SingleShoot++;
                            CompetitionState = CompetitionState.Prepare;
                        }
                    }
                    else 
                    {
                        if (RunNumber >= Shooting.CompetitionProperties.CompetitionRuns.Count())
                        {
                            RunNumber = 0;
                            CompetitionState = CompetitionState.Result;
                        }
                        else
                        {
                            RunNumber++;
                            CompetitionState = CompetitionState.Prepare;
                        }
                    }
                    break;
            }
        }

        private async void LoadItemId(string itemId)
        {
            try
            {
                IsBusy = true;

                Guid id = Guid.Parse(itemId);
                if (id == Guid.Empty)
                    throw new Exception("Wrong id");

                if (CompetitionDataStore == null)
                    throw new Exception("DataStore is null");

                Shooting = await CompetitionDataStore.GetItemAsync(id);

                if (Shooting == null || Shooting.CompetitionProperties == null)
                    throw new Exception("ShootingCompetition not allowed to be null");

                GenerateRunTemplate();

                ContinueCommand.ChangeCanExecute();

                IsBusy = false;
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to Load Item");
            }
        }

        private bool CanExecuteStop(object arg)
        {
            return true;
        }

        private async void OnStop(object obj)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Stopp Wettkampf",
                "Willst du den Wettkampf stoppen?",
                "Ja",
                "Nein");

            if (confirm)
            {
                Shooting.EndTime = DateTime.Now;

                await CompetitionDataStore.UpdateItemAsync(Shooting);

                await Shell.Current.GoToAsync($"{nameof(CompetitionsPage)}", true);
            }
        }

        private bool CanExecuteSave(object arg)
        {
            var isDone = CheckAllValuationsDone();

            if (isDone == false)
                return false;

            return true;
        }

        private async void OnSave(object obj)
        {
            List<Run> runs = new();

            if (Shooting?.Runs != null)
                runs = [.. Shooting.Runs];


            Run run = new()
            {
                RunNumber = RunNumber,
                BulletNumber = Bullets.Count(),
                Time = DateTime.Now,
                ShootingId = Shooting?.Id ?? Guid.Empty,
            };

            List<int> valuations = [];

            foreach (var bullet in Bullets)
            {
                if (bullet.Valuations == null)
                    throw new Exception("Valuations can not be null here");

                valuations.Add(bullet.Valuations.FirstOrDefault(n => n.Hit)?.Value ?? 0);
            }

            run.Results = valuations;
            runs.Add(run);

            if (Shooting == null)
                throw new Exception("Shooting not allowed to be null here");

            Shooting.Runs = runs;

            await CompetitionDataStore.UpdateItemAsync(Shooting);

            GenerateRunTemplate();

            StopCommand.ChangeCanExecute();
            SaveCommand.ChangeCanExecute();
        }

        private bool CheckAllValuationsDone()
        {
            if (Bullets.Count() == 0)
                return false;

            foreach (var bullet in Bullets)
            {
                var done = bullet.Valuations?.FirstOrDefault(n => n.Hit == true);

                if (done == null)
                    return false;
            }

            return true;
        }

        private void GenerateRunTemplate()
        {
            List<BulletTemplate> bullets = new List<BulletTemplate>();

            for (int bullet = 0; bullet < Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).NumberOfBullets; bullet++)
            {
                bullets.Add(new BulletTemplate { BulletNumber = bullet + 1, Valuations = GenerateValuationTemplates() });
            }

            Bullets = bullets;
        }

        private List<ValuationTemplate> GenerateValuationTemplates()
        {
            if (Shooting.TargetProperties?.Valuations == null)
                throw new Exception("Valuations not found");

            List<ValuationTemplate> templates = new();
            foreach (int assesment in Shooting.TargetProperties.Valuations)
            {
                templates.Add(new ValuationTemplate { Value = assesment, Hit = false });
            }

            return templates;
        }

        public class BulletTemplate
        {
            public int BulletNumber { get; set; }
            public IEnumerable<ValuationTemplate>? Valuations { get; set; }
        }

        public class ValuationTemplate
        {
            public int Value { get; set; }
            public bool Hit { get; set; }
        }
    }
}
