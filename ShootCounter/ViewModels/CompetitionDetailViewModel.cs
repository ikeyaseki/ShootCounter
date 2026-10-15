using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using ShootCounter.Helpers;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingCompetitionId), nameof(ShootingCompetitionId))]
    public class CompetitionDetailViewModel : BaseViewModel
    {
        private readonly IDataStore<ShootingCompetition> _competitionDataStore;
        private string _shootingCompetitionId = string.Empty;
        private ShootingCompetition _shooting = new();
        private IEnumerable<ISeries> _runSeries = new List<ISeries>();
        private Axis[] _xAxes = [];

        public Command DeleteCommand { get; private set; }

        public string ShootingCompetitionId 
        { 
            get => _shootingCompetitionId; 
            set
            {
                SetProperty(ref _shootingCompetitionId, value);
                LoadShootingsAsync(value);
            }
        }

        public IEnumerable<ISeries> RunSeries
        {
            get => _runSeries;
            set
            {
                SetProperty(ref _runSeries, value);
            }
        }

        public Axis[] XAxes
        {
            get => _xAxes;
            set
            {
                SetProperty(ref _xAxes, value);
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

        public CompetitionDetailViewModel(IDataStore<ShootingCompetition> competitionDataStore)
        {
            _competitionDataStore = competitionDataStore;
            Title = "Wettkampf Details";
            DeleteCommand = new Command(async () => await ExecuteDelete(), CanExecuteDelete);
        }

        private bool CanExecuteDelete()
        {
            return Shooting != null;
        }

        private async Task ExecuteDelete()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Wettkampf löschen",
                "Willst du den Wettkampf wirklich löschen?",
                "Ja",
                "Nein");

            if (confirm)
            {
                await _competitionDataStore.DeleteItemAsync(Shooting.Id);

                await Shell.Current.GoToAsync($"//{nameof(CompetitionsPage)}", true);
            }
        }

        private async void LoadShootingsAsync(string id)
        {
            try
            {
                IsBusy = true;

                var shooting = await _competitionDataStore.GetItemAsync(Guid.Parse(id));

                if (shooting == null)
                    return;

                CalculateShooting calculate = new();
                Shooting = await calculate.CalculateCompetition(shooting);

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
