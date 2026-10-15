using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ShootCounter.Helpers;
using SkiaSharp;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class TrainingDetailViewModel : BaseViewModel
    {
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
        private Axis[] _xAxes;

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

        public TrainingDetailViewModel()
        {
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
                await TrainingDataStore.DeleteItemAsync(Shooting.Id);

                await Shell.Current.GoToAsync($"//{nameof(TrainingsPage)}", true);
            }
        }

        private async void LoadShootingsAsync(string id)
        {
            try
            {
                IsBusy = true;

                var shooting = await TrainingDataStore.GetItemAsync(Guid.Parse(id));

                if (shooting == null)
                    return;

                CalculateShooting calculate = new();
                Shooting = await calculate.CalculateTraining(shooting);

                // a training stopped before the first run has no runs, Max() would throw on an empty list
                var runResults = Shooting.Runs?.Select(n => n.Results?.ToList() ?? []).ToList() ?? [];
                if (runResults.Count == 0)
                    return;

                XAxes =
                [
                    new Axis
                    {
                        Name = "Passen",
                        Labels = Enumerable.Range(1, runResults.Count).Select(x => x.ToString()).ToList(),
                    }
                ];

                var maxLength = runResults.Max(n => n.Count);

                RunSeries = Enumerable.Range(0, maxLength + 1)
                    .Select(i => new ColumnSeries<int>
                    {
                        Name = i >= maxLength ? "Platzhalter" : $"Schuss {i + 1}",
                        Values = runResults.Select(results => i < results.Count ? results[i] : 0).ToList(),
                        Fill = new SolidColorPaint(SKColors.BlueViolet),
                    }).ToList();
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
