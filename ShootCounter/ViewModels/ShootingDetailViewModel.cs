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
                        TrefferIndex = index,
                        Wert = wert
                    }))
                    .ToList();

                Drawable = new BarChartDrawable(punkte);

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
        public int TrefferIndex { get; set; }
        public int Wert { get; set; }
    }

    public class BarChartDrawable : IDrawable
    {
        private List<ChartDataPoint> _punkte;

        public BarChartDrawable(List<ChartDataPoint> punkte)
        {
            _punkte = punkte;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var gruppiert = _punkte.GroupBy(p => p.RunNumber).ToList();
            float chartWidth = dirtyRect.Width;
            float chartHeight = dirtyRect.Height;
            float barWidth = 20;
            float spacing = 10;
            float groupSpacing = 40;
            float maxValue = _punkte.Max(p => p.Wert);

            float x = spacing;

            foreach (var gruppe in gruppiert)
            {
                foreach (var punkt in gruppe)
                {
                    float barHeight = (float)(punkt.Wert / maxValue) * (chartHeight - 50);
                    canvas.FillColor = Colors.Blue;
                    canvas.FillRectangle(x, chartHeight - barHeight, barWidth, barHeight);

                    canvas.DrawString(
                        $"R{punkt.RunNumber}-T{punkt.TrefferIndex + 1}",
                        x, chartHeight - 25, barWidth, 20,
                        HorizontalAlignment.Center,
                        VerticalAlignment.Top,
                        TextFlow.OverflowBounds,
                        10);

                    x += barWidth + spacing;
                }

                x += groupSpacing;
            }
        }
    }
}
