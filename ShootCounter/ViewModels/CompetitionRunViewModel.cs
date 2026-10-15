using ShootCounter.Helpers;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class CompetitionRunViewModel : BaseViewModel
    {
        private readonly IDataStore<ShootingCompetition> _competitionDataStore;
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
        private Timer? _timer;
        private string _prepareLoadingString = string.Empty;
        private string _prepareCommandString = string.Empty;
        private string _resultString = string.Empty;
        private bool _isContinuing;

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
        public string ResultString
        {
            get => _resultString;
            set
            {
                SetProperty(ref _resultString, value);
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

        public CompetitionRunViewModel(IDataStore<ShootingCompetition> competitionDataStore)
        {
            _competitionDataStore = competitionDataStore;
            Title = "Wettkampf";
            CompetitionState = CompetitionState.Info;
            IsEnabledChange();
            StopCommand = new Command(OnStop, CanExecuteStop);
            ContinueCommand = new Command(OnContinue, CanExecuteContinue);
        }

        private bool CanExecuteContinue()
        {
            if (CompetitionState != CompetitionState.CountPoints)
                return true;

            return CheckAllValuationsDone();
        }

        private async void OnContinue()
        {
            // a fast double tap would otherwise save the same run twice
            if (_isContinuing)
                return;

            _isContinuing = true;
            try
            {
                await StateChange();
                IsEnabledChange();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Der Wettkampf konnte nicht gespeichert werden.", "OK");
            }
            finally
            {
                _isContinuing = false;
            }
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

        private async Task StateChange()
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
                    _timer = null;
                    SecondsTimer = 0;
                    GenerateRunTemplate();
                    CompetitionState = CompetitionState.CountPoints;
                    break;

                case CompetitionState.CountPoints:
                    await SaveAsync();

                    if (Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt)
                    {
                        if (SingleShoot >= Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).NumberOfBullets)
                        {
                            if (RunNumber >= Shooting.CompetitionProperties.CompetitionRuns.Count())
                            {
                                SingleShoot = 0;
                                RunNumber = 0;
                                await CalculateResult();
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
                            await CalculateResult();
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

                case CompetitionState.Result:
                    await Shell.Current.GoToAsync($"//{nameof(CompetitionsPage)}", true);
                    break;
            }

            ContinueCommand.ChangeCanExecute();
        }

        private async Task CalculateResult()
        {
            CalculateShooting calculateShooting = new CalculateShooting();

            Shooting = await calculateShooting.CalculateCompetition(Shooting);

            ResultString = $"Du hast {Shooting.Result} Punkte von maximal {Shooting.CompetitionProperties.MaxPoints} gemacht.";

            var numberOfZeros = Shooting.Runs.SelectMany(n => n.Results).Where(n => n == 0).Count();

            

            if (Shooting.CompetitionProperties.CompetitionType == CompetitionType.Obligatorisch25m)
            {
                var minPoints = Shooting.CompetitionProperties.MinPoints.Value;
                var maxZeros = Shooting.CompetitionProperties.MaxZeros.Value;

                var failPoints = Shooting.Result < minPoints;
                var failZeros = numberOfZeros > maxZeros;

                if (failPoints)
                    ResultString = $"{ResultString}\n\nDu hast nicht bestanden da zu wenig Punkte.";

                if (failZeros)
                    ResultString = $"{ResultString}\n\nDu hast nicht bestanden da zuviele 0er.";

                if (failPoints || failZeros)
                    return;

                if (!failPoints && !failZeros)
                    ResultString = $"{ResultString}\n\nDu hast bestanden.";
            }

            var kranz = Shooting.CompetitionProperties.KranzPoints;
            if (kranz.HasValue)
            {
                if (Shooting.Result >= kranz.Value)
                {
                    ResultString = $"{ResultString}\n\nDu hast den Kranz geholt, Gratulation!";
                    return;
                }
            }

            var karte = Shooting.CompetitionProperties.KartePoints;
            if (karte.HasValue)
            {
                if (Shooting.Result >= karte.Value)
                {
                    ResultString = $"{ResultString}\n\nDu hast eine Anerkennungskarte geholt, Gratulation!";
                }
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

                Shooting = await _competitionDataStore.GetItemAsync(id);

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
                // stop the timer, otherwise it keeps ticking after the page is left
                _timer?.Dispose();
                _timer = null;

                Shooting.EndTime = DateTime.Now;

                await _competitionDataStore.UpdateItemAsync(Shooting);

                await Shell.Current.GoToAsync($"//{nameof(CompetitionsPage)}", true);
            }
        }

        private async Task SaveAsync()
        {
            List<Run> runs = new();

            if (Shooting?.Runs != null)
                runs = [.. Shooting.Runs];

            if (Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt)
            {
                if (SingleShoot == 1)
                {
                    Run run = new()
                    {
                        RunNumber = RunNumber,
                        BulletNumber = 1,
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
                }
                else
                {
                    Run run = Shooting.Runs.First(n => n.RunNumber == RunNumber);
                    run.BulletNumber++;
                    run.Time = DateTime.Now;

                    List<int> valuations = run.Results.ToList();

                    foreach (var bullet in Bullets)
                    {
                        if (bullet.Valuations == null)
                            throw new Exception("Valuations can not be null here");

                        valuations.Add(bullet.Valuations.FirstOrDefault(n => n.Hit)?.Value ?? 0);
                    }

                    run.Results = valuations;
                    runs[RunNumber - 1] = run;
                }
            }
            else
            {
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
            }

            if (Shooting == null)
                throw new Exception("Shooting not allowed to be null here");

            Shooting.Runs = runs;
            Shooting.EndTime = DateTime.Now;

            await _competitionDataStore.UpdateItemAsync(Shooting);
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

            if (Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).Einzelgezeigt)
            {
                bullets.Add(new BulletTemplate { BulletNumber = SingleShoot, Valuations = GenerateValuationTemplates() });
            }
            else
            {
                for (int bullet = 0; bullet < Shooting.CompetitionProperties.CompetitionRuns.First(n => n.RunNumber == RunNumber).NumberOfBullets; bullet++)
                {
                    bullets.Add(new BulletTemplate { BulletNumber = bullet + 1, Valuations = GenerateValuationTemplates() });
                }
            }

            Bullets = bullets;
        }

        private List<ValuationTemplate> GenerateValuationTemplates() => ValuationTemplate.CreateList(Shooting.TargetProperties);
    }
}
