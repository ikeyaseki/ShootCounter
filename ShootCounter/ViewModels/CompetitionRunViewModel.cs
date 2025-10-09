using ShootCounter.Helpers;
using System.Diagnostics;

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
        private CompetitionState _competitionState;
        private int _singleShoot = 0;
        private string _singleShootString = string.Empty;
        private bool _isEnabledInfo;
        private bool _isEnabledTrialShots;
        private bool _isEnabledPrepare;
        private bool _isEnabledShooting;
        private bool _isEnabledCountPoints;
        private bool _isEnabledResult;
        private int _secondsTimer = 0;
        private int _timerDuration;
        private WarningState _warningState;
        private Timer _timer;
        private string _prepareLoadingString = string.Empty;
        private string _prepareCommandString = string.Empty;

        public Command SaveCommand { get; }
        public Command StopCommand { get; }
        public Command ContinueCommand { get; }

        public WarningState WarningState 
        {
            get => _warningState;
            set
            {
                SetProperty(ref _warningState, value);
            }
        }

        public int SecondsTimer
        {
            get => _secondsTimer;
            set
            {
                SetProperty(ref _secondsTimer, value);
            }
        }

        public bool IsEnabledInfo
        {
            get => _isEnabledInfo;
            set
            {
                SetProperty(ref _isEnabledInfo, value);
            }
        }

        public bool IsEnabledTrialShots
        {
            get => _isEnabledTrialShots;
            set
            {
                SetProperty(ref _isEnabledTrialShots, value);
            }
        }

        public bool IsEnabledPrepare
        {
            get => _isEnabledPrepare;
            set
            {
                SetProperty(ref _isEnabledPrepare, value);
            }
        }

        public bool IsEnabledShooting
        {
            get => _isEnabledShooting;
            set
            {
                SetProperty(ref _isEnabledShooting, value);
            }
        }

        public bool IsEnabledCountPoints
        {
            get => _isEnabledCountPoints;
            set
            {
                SetProperty(ref _isEnabledCountPoints, value);
            }
        }

        public bool IsEnabledResult
        {
            get => _isEnabledResult;
            set
            {
                SetProperty(ref _isEnabledResult, value);
            }
        }

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

        public string PrepareLoadingString
        {
            get => _prepareLoadingString;
            set
            {
                SetProperty(ref _prepareLoadingString, value);
            }
        }

        public string PrepareCommandString
        {
            get => _prepareCommandString;
            set
            {
                SetProperty(ref _prepareCommandString, value);
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
            IsEnabledChange();
            SaveCommand = new Command(OnSave, CanExecuteSave);
            StopCommand = new Command(OnStop, CanExecuteStop);
            ContinueCommand = new Command(OnContinue, CanExecuteContinue);

            Shell.Current.Navigating += (s, e) =>
            {
                if (e.Source == ShellNavigationSource.Pop && e.CanCancel)
                {
                    e.Cancel();
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
            IsEnabledChange();
        }

        private void IsEnabledChange()
        {
            switch (CompetitionState)
            {
                case CompetitionState.Info:
                    IsEnabledInfo = true;
                    IsEnabledTrialShots = false;
                    IsEnabledPrepare = false;
                    IsEnabledShooting = false;
                    IsEnabledCountPoints = false;
                    IsEnabledResult = false;
                    break;
                case CompetitionState.TrialShots:
                    IsEnabledInfo = false;
                    IsEnabledTrialShots = true;
                    IsEnabledPrepare = false;
                    IsEnabledShooting = false;
                    IsEnabledCountPoints = false;
                    IsEnabledResult = false;
                    break;
                case CompetitionState.Prepare:
                    IsEnabledInfo = false;
                    IsEnabledTrialShots = false;
                    IsEnabledPrepare = true;
                    IsEnabledShooting = false;
                    IsEnabledCountPoints = false;
                    IsEnabledResult = false;
                    break;
                case CompetitionState.Shooting:
                    IsEnabledInfo = false;
                    IsEnabledTrialShots = false;
                    IsEnabledPrepare = false;
                    IsEnabledShooting = true;
                    IsEnabledCountPoints = false;
                    IsEnabledResult = false;
                    break;
                case CompetitionState.CountPoints:
                    IsEnabledInfo = false;
                    IsEnabledTrialShots = false;
                    IsEnabledPrepare = false;
                    IsEnabledShooting = false;
                    IsEnabledCountPoints = true;
                    IsEnabledResult = false;
                    break;
                case CompetitionState.Result:
                    IsEnabledInfo = false;
                    IsEnabledTrialShots = false;
                    IsEnabledPrepare = false;
                    IsEnabledShooting = false;
                    IsEnabledCountPoints = false;
                    IsEnabledResult = true;
                    break;
            }
        }

        private void StateChange()
        {
            switch (CompetitionState)
            {
                case CompetitionState.Info:
                    if (Shooting.CompetitionProperties.TrialShots == true)
                    {
                        CompetitionState = CompetitionState.TrialShots;
                    }
                    else
                    {
                        RunNumber = 1;

                        if (Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt)
                            SingleShoot = 1;

                        CreatePrepareStrings();

                        CompetitionState = CompetitionState.Prepare;
                    }
                    break;
                case CompetitionState.TrialShots:
                    RunNumber = 1;

                    if (Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt)
                        SingleShoot = 1;

                    CreatePrepareStrings();

                    CompetitionState = CompetitionState.Prepare;
                    break;
                case CompetitionState.Prepare:
                    int? duration = Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Duration;

                    if (duration.HasValue)
                        StartTimer(duration.Value);

                    CompetitionState = CompetitionState.Shooting;
                    break;
                case CompetitionState.Shooting:
                    _timer?.Dispose();
                    SecondsTimer = 0;
                    GenerateRunTemplate();
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
                                CreatePrepareStrings();
                                CompetitionState = CompetitionState.Prepare;
                            }
                        }
                        else
                        {
                            SingleShoot++;
                            CreatePrepareStrings();
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
                            CreatePrepareStrings();
                            CompetitionState = CompetitionState.Prepare;
                        }
                    }
                    break;
            }
        }

        private void CreatePrepareStrings()
        {
            int? duration = Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Duration;
            bool isSingleShot = Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt;
            int numberOfBullets = Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).NumberOfBullets;

            if (isSingleShot)
            {
                PrepareLoadingString = "1 Schuss laden";

                PrepareCommandString = duration.HasValue
                    ? $"Für 1 Schuss Einzelfeuer in {duration} Sekunden, ist der Schütze bereit?"
                    : $"Für 1 Schuss Einzelfeuer ohne Zeitlimite, ist der Schütze bereit?";
            }
            else
            {
                PrepareLoadingString = $"{numberOfBullets} Schuss laden";

                PrepareCommandString = duration.HasValue
                    ? $"Für {numberOfBullets} Schuss Schnellfeuer in {duration} Sekunden, ist der Schütze bereit?"
                    : $"Für {numberOfBullets} Schuss Schnellfeuer ohne Zeitlimite, ist der Schütze bereit?";
            }
        }

        private void StartTimer(int value)
        {
            _timerDuration = value;
            WarningState = WarningState.Ok;
            _timer = new Timer(OnTimerTick, null, 0, 1000);
        }

        private void OnTimerTick(object? state)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                SecondsTimer++;
                if (SecondsTimer > _timerDuration)
                    WarningState = WarningState.Warning;
            });
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
