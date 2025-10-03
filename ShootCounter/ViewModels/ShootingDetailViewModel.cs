using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    [QueryProperty(nameof(ShootingId), nameof(ShootingId))]
    public class ShootingDetailViewModel : BaseViewModel
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
        private readonly ShootingsViewModel _shootingsViewModel;

        public Command DeleteCommand { get; private set; }

        public string Text { get; set; } = "Shooting details";

        public string ShootingId
        {
            get => _shootingId;
            set
            {
                _shootingId = value;
                LoadShootingsAsync(value);
            }
        }

        public ShootingDetailViewModel(ShootingsViewModel shootingsViewModel)
        {
            _shootingsViewModel = shootingsViewModel;
            Title = "Shooting Detail";
            DeleteCommand = new Command(async () => await ExecuteDelete(), CanExecuteSave);
        }

        private bool CanExecuteSave()
        {
            return Shooting != null;
        }

        private async Task ExecuteDelete()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Stop shooting",
                "Are you shure to delete shooting?",
                "Yes",
                "No");

            if (confirm)
            {
                await DataStore.DeleteItemAsync(Shooting.Id);

                await Shell.Current.GoToAsync($"{nameof(ShootingsPage)}", true);
            }
        }

        private async void LoadShootingsAsync(string id)
        {
            try
            {
                IsBusy = true;

                var shooting = await DataStore.GetItemAsync(Guid.Parse(id));

                if (shooting == null)
                {
                    IsBusy = false;
                    return;
                }

                CalculateShooting calculate = new();
                Shooting = await calculate.Calculate(shooting);

                if (Shooting.Runs == null)
                {
                    IsBusy = false;
                    return;
                }

                List<string> xAxisLabels = new List<string>();
                for (int x = 1; x <= Shooting.Runs.Count(); x++ )
                {
                   xAxisLabels.Add(x.ToString());
                }

                XAxes = new List<Axis>
                {
                    new Axis
                    {
                        Name = "Runs",
                        Labels = xAxisLabels.ToList(),
                    }
                }.ToArray();

                var maxLength = Shooting.Runs.Max(n => n.Results.Count());       

                RunSeries = Enumerable.Range(0, maxLength + 1)
                    .Select(i => new ColumnSeries<int>
                    {
                        Name = i >= maxLength ? "Placeholder" : $"Bullet {i + 1}",
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
