using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ShootCounter.Helpers;
using SkiaSharp;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingCompetitionId), nameof(ShootingCompetitionId))]
    public class CompetitionDetailViewModel : BaseViewModel
    {
        private string _shootingCompetitionId = string.Empty;
        private ShootingCompetition _shooting = new();
        private IEnumerable<ISeries> _runSeries = new List<ISeries>();
        private Axis[] _xAxes;

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

        public CompetitionDetailViewModel()
        {
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
                await CompetitionDataStore.DeleteItemAsync(Shooting.Id);

                await Shell.Current.GoToAsync($"{nameof(CompetitionsPage)}", true);
            }
        }

        private async void LoadShootingsAsync(string id)
        {
            try
            {
                IsBusy = true;

                var shooting = await CompetitionDataStore.GetItemAsync(Guid.Parse(id));

                if (shooting == null)
                {
                    IsBusy = false;
                    return;
                }

                CalculateShooting calculate = new();
                Shooting = await calculate.CalculateCompetition(shooting);

                if (Shooting.Runs == null)
                {
                    IsBusy = false;
                    return;
                }

                List<string> xAxisLabels = new List<string>();
                for (int x = 1; x <= Shooting.Runs.Count(); x++)
                {
                    xAxisLabels.Add(x.ToString());
                }

                XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = "Passen",
                        Labels = xAxisLabels.ToList(),
                    }
                }.ToArray();

                var maxLength = Shooting.Runs.Max(n => n.Results.Count());

                RunSeries = Enumerable.Range(0, maxLength + 1)
                    .Select(i => new ColumnSeries<int>
                    {
                        Name = i >= maxLength ? "Platzhalter" : $"Schuss {i + 1}",
                        Values = Shooting.Runs.Select(run =>
                            i < run.Results.Count() ? run.Results.ElementAt(i) : 0).ToList(),
                        Fill = new SolidColorPaint(SKColors.BlueViolet),
                    }).ToList();

                IsBusy = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            DeleteCommand.ChangeCanExecute();
        }
    }
}
