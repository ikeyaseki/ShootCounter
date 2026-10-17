using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ShootCounter.Helpers;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class TrainingDetailViewModel : BaseViewModel
    {
        private readonly IDataStore<Shooting> _trainingDataStore;
        public IEnumerable<ISeries> RunSeries 
        { 
            get => _runSeries; 
            set
            {
                SetProperty(ref _runSeries, value);
            }
        }

        // the points start at 0, the columns of the runs are comparable

        public Axis[] YAxes { get; } = [ChartTheme.Apply(new Axis { MinLimit = 0 })];


        public Axis[] XAxes 
        { 
            get => _xAxes; 
            set
            {
                SetProperty(ref _xAxes, value);
            }
        }

        public Shooting Shooting 
        { 
            get => _shooting; 
            set
            {
                SetProperty(ref _shooting, value);
            }
        }

        private string _shootingId = string.Empty;
        private Shooting _shooting = new();
        private IEnumerable<ISeries> _runSeries = new List<ISeries>();
        private Axis[] _xAxes = [];

        public Command DeleteCommand { get; private set; }

        public string ShootingId
        {
            get => _shootingId;
            set
            {
                _shootingId = value;
                LoadShootingsAsync(value);
            }
        }

        public TrainingDetailViewModel(IDataStore<Shooting> trainingDataStore)
        {
            _trainingDataStore = trainingDataStore;
            Title = "Training Details";
            DeleteCommand = new Command(async () => await ExecuteDelete(), CanExecuteDelete);
        }

        private bool CanExecuteDelete()
        {
            return Shooting != null;
        }

        private async Task ExecuteDelete()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Training löschen",
                "Willst du das Training wirklich löschen?",
                "Ja",
                "Nein");

            if (confirm)
            {
                await _trainingDataStore.DeleteItemAsync(Shooting.Id);

                await Shell.Current.GoToAsync($"//{nameof(TrainingsPage)}", true);
            }
        }

        private async void LoadShootingsAsync(string id)
        {
            try
            {
                IsBusy = true;

                var shooting = await _trainingDataStore.GetItemAsync(Guid.Parse(id));

                if (shooting == null)
                    return;

                CalculateShooting calculate = new();
                Shooting = await calculate.CalculateTraining(shooting);

                if (RunChart.TryCreate(Shooting.Runs, out var xAxes, out var runSeries))
                {
                    XAxes = xAxes;
                    RunSeries = runSeries;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }

            DeleteCommand.ChangeCanExecute();
        }
    }
}
