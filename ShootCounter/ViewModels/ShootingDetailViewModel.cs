using Kotlin;
using Microsoft.Maui.Graphics;
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
        public IDrawable Drawable 
        { 
            get => _drawable; 
            set
            {
                SetProperty(ref _drawable, value);
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
        private IDrawable _drawable;
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
                CalculateShooting calculate = new();
                Shooting = await calculate.Calculate(shooting);

                var punkte = Shooting.Runs
                    .Where(r => r.Results != null)
                    .SelectMany(r => r.Results!.Select((wert, index) => new ChartDataPoint
                    {
                        RunNumber = r.RunNumber,
                        HitIndex = index,
                        Valuation = wert
                    }))
                    .ToList();

                Drawable = new BarChartDrawable(punkte, shooting.TargetProperties.Valuations.Max());

                    IsBusy = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            DeleteCommand.ChangeCanExecute();
        }
    }
    public class ChartDataPoint
    {
        public int RunNumber { get; set; }
        public int HitIndex { get; set; }
        public int Valuation { get; set; }
    }

    public class BarChartDrawable : IDrawable
    {
        private List<ChartDataPoint> _punkte;
        private readonly float _maxValue;

        public BarChartDrawable(List<ChartDataPoint> punkte, float maxValue)
        {
            _punkte = punkte;
            _maxValue = maxValue;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var gruppiert = _punkte.GroupBy(p => p.RunNumber).ToList();
            float chartWidth = dirtyRect.Width;
            float chartHeight = dirtyRect.Height - 100;
            float spacing = 10;
            float groupSpacing = 40;
            float maxValue = _punkte.Max(p => p.Valuation);

            float offset = 40;
            float totalBars = gruppiert.Sum(g => g.Count());
            float totalSpacing = (totalBars - 1) * spacing + (gruppiert.Count - 1) * groupSpacing;
            float barWidth = (chartWidth - totalSpacing - offset - 20) / totalBars;


            float x = spacing + offset;

            foreach (var gruppe in gruppiert)
            {
                var totalValuation = 0;
                var numberOfHits = 0;
                var runNumber = 0;

                foreach (var punkt in gruppe)
                {
                    float barHeight = (float)(punkt.Valuation / _maxValue) * (chartHeight - 20);
                    canvas.FillColor = Colors.Blue;
                    canvas.FillRectangle(x, chartHeight - barHeight, barWidth, barHeight);

                    totalValuation += punkt.Valuation;
                    numberOfHits++;
                    runNumber = punkt.RunNumber;

                    x += barWidth + spacing;
                }

                var percentage = Math.Round(totalValuation / (numberOfHits * _maxValue) * 100);
                canvas.FontSize = 16;
                canvas.DrawString(
                        $"{runNumber}: {percentage}%",
                        x - (numberOfHits * (barWidth + spacing)), chartHeight + 20, 60, 30,
                        HorizontalAlignment.Center,
                        VerticalAlignment.Top,
                        TextFlow.OverflowBounds);

                x += groupSpacing;
            }


            for (int i = 0; i <= 5; i++)
            {
                float value = _maxValue * i / 5;

                float y = chartHeight - (i / 5f) * (chartHeight - 20);
                canvas.FontSize = 16;
                canvas.DrawString($"{value:0}", 0, y - 10, 40, 20, HorizontalAlignment.Left, VerticalAlignment.Top, TextFlow.OverflowBounds);
                canvas.DrawLine(40, y, chartWidth, y);
            }
        }
    }
}
