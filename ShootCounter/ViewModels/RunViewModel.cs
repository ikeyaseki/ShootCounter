using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class RunViewModel : BaseViewModel
    {
        private readonly IDataStore<Shooting> _trainingDataStore;
        private string _shootingId = string.Empty;
        private Shooting _shooting = new();
        private IEnumerable<BulletTemplate> _bullets = [];
        private int _runNumber;
        private string _result = string.Empty;
        private string _percent = string.Empty;
        private bool _isSaving;

        public Command SaveCommand { get; }
        public Command AddBulletCommand { get; }
        public Command RemoveBulletCommand { get; }
        public Command StopCommand { get; }

        public string ShootingId
        {
            get => _shootingId;
            set
            {
                _shootingId = value;
                LoadItemId(value);
            }
        }

        public IEnumerable<BulletTemplate> Bullets 
        { 
            get => _bullets; 
            set
            {
                SetProperty(ref _bullets, value);
                RemoveBulletCommand.ChangeCanExecute();
            }
        }

        public string Result 
        { 
            get => _result; 
            set
            {
                SetProperty(ref _result, value);
            }
        }

        public string Percent 
        { 
            get => _percent; 
            set
            {
                SetProperty(ref _percent, value);
            }
        }

        public RunViewModel(IDataStore<Shooting> trainingDataStore)
        {
            _trainingDataStore = trainingDataStore;
            RunNumber = 0;
            SaveCommand = new Command(OnSave, CanExecuteSave);
            AddBulletCommand = new Command(OnAddBullet);
            RemoveBulletCommand = new Command(OnRemoveBullet, CanExecuteRemove);
            StopCommand = new Command(OnStop, CanExecuteStop);
        }

        private bool CanExecuteStop(object arg)
        {
            return CheckNoValuationsDone();
        }

        private async void OnStop(object obj)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Stopp Training",
                "Willst du das Training stoppen?",
                "Ja",
                "Nein");

            if (confirm)
            {
                _shooting.EndTime = DateTime.Now;

                await _trainingDataStore.UpdateItemAsync(_shooting);

                await Shell.Current.GoToAsync($"//{nameof(TrainingsPage)}", true);
            }
        }

        private bool CanExecuteRemove(object arg)
        {
            return Bullets.Count() > 0;
        }

        private void OnRemoveBullet(object obj)
        {
            List<BulletTemplate> newBullets = [.. Bullets];
            newBullets.RemoveAt(Bullets.Count() - 1);
            Bullets = newBullets;
        }

        private void OnAddBullet(object obj)
        {
            List<BulletTemplate> newBullets = [.. Bullets];
            newBullets.Add(new BulletTemplate { BulletNumber = Bullets.Count() + 1, Valuations = GenerateValuationTemplates() });
            Bullets = newBullets;
        }

        private bool CanExecuteSave(object arg)
        {
            Result = string.Empty;
            Percent = string.Empty;

            var isDone = CheckAllValuationsDone();

            if (isDone == false)
                return false;

            CalculateRunResult();

            return true;
        }

        private void CalculateRunResult()
        {
            // get max valuation
            if (Bullets.First().Valuations == null)
                throw new Exception("Valuations can not be null here");

            int maxValuation = Bullets.First().Valuations?.Max(n => n.Value) ?? 0;

            double maxResult = maxValuation * Bullets.Count();

            int result = 0;
            foreach (var bullet in Bullets)
            {
                result += bullet?.Valuations?.FirstOrDefault(n => n.Hit)?.Value ?? 0;
            }

            double score = result / maxResult * 100;
            score = Math.Round(score, 0);

            Result = $"Treffer {result} von {maxResult}";
            Percent = $"{score}%";
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

        private bool CheckNoValuationsDone()
        {
            if (Bullets.Count() == 0)
                return true;

            foreach (var bullet in Bullets)
            {
                var done = bullet.Valuations?.FirstOrDefault(n => n.Hit == true);

                if (done != null)
                    return false;
            }

            return true;

        }

        private async void OnSave(object obj)
        {
            // a fast double tap would otherwise save the same run twice
            if (_isSaving)
                return;

            _isSaving = true;
            try
            {
                await SaveRunAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Die Passe konnte nicht gespeichert werden.", "OK");
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task SaveRunAsync()
        {
            List<Run> runs = new();

            if (_shooting?.Runs != null)
                runs = [.. _shooting.Runs];


            Run run = new()
            {
                RunNumber = RunNumber,
                BulletNumber = Bullets.Count(),
                Time = DateTime.Now,
                ShootingId = _shooting?.Id ?? Guid.Empty,
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

            if (_shooting == null)
                throw new Exception("Shooting not allowed to be null here");

            _shooting.Runs = runs;
            // keep the end time current, so the duration is correct even if the training is never stopped
            _shooting.EndTime = DateTime.Now;

            await _trainingDataStore.UpdateItemAsync(_shooting);

            GenerateRunTemplate();

            StopCommand.ChangeCanExecute();
            SaveCommand.ChangeCanExecute();
        }

        public int RunNumber 
        { 
            get => _runNumber; 
            set
            {
                Title = $"Passe {value}";
                SetProperty(ref _runNumber, value);
            }
        }

        private async void LoadItemId(string itemId)
        {
            try
            {
                IsBusy = true;
                _shooting = await _trainingDataStore.GetItemAsync(Guid.Parse(itemId));
                GenerateRunTemplate();
                IsBusy = false;
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to Load Item");
            }
        }

        private void GenerateRunTemplate()
        {
            List<BulletTemplate> bullets = new List<BulletTemplate>();

            for(int bullet = 0; bullet < _shooting.BulletsPerRun; bullet++)
            {
                bullets.Add(new BulletTemplate { BulletNumber = bullet + 1, Valuations = GenerateValuationTemplates() });
            }

            Bullets = bullets;
            RunNumber++;
        }

        private List<ValuationTemplate> GenerateValuationTemplates() => ValuationTemplate.CreateList(_shooting.TargetProperties);
    }
}
