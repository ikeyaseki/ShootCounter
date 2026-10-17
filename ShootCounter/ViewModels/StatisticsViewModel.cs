using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ShootCounter.Helpers;
using SkiaSharp;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    public record PeriodOption(string Name, StatisticsPeriod Period);

    public record WeaponOption(string Name, Guid? WeaponId);

    public record CaliberOption(string Name, string? CaliberKey);

    // a row of the weapon table with the texts ready for display
    public record WeaponResultRow(string WeaponName, string Training, string Competition, int Shots);

    public class StatisticsViewModel : BaseViewModel
    {
        private readonly StatisticsService _statisticsService;
        private IReadOnlyList<StatisticsEntry> _entries = [];
        private bool _isLoading;

        private PeriodOption _selectedPeriod;
        private DateTime _customFrom = DateTime.Today.AddDays(-30);
        private DateTime _customTo = DateTime.Today;
        private WeaponOption _selectedWeapon;
        private CaliberOption _selectedCaliber;
        private IReadOnlyList<WeaponOption> _weaponOptions;
        private IReadOnlyList<CaliberOption> _caliberOptions;

        private string _totalShots = "0";
        private string _trainingShots = "0";
        private string _competitionShots = "0";
        private string _trainingCount = "0";
        private string _competitionCount = "0";
        private string _trainingPercentage = NoValue;
        private string _competitionPercentage = NoValue;
        private bool _hasData;
        private ISeries[] _shotSeries = [];
        private Axis[] _xAxes = [];
        private Axis[] _yAxes = [];
        private SolidColorPaint _legendTextPaint = ChartTheme.LegendText;
        private string _chartTitle = string.Empty;
        private IReadOnlyList<ShotsBucket> _days = [];
        private IReadOnlyList<WeaponResultRow> _weaponResults = [];
        private ISeries[] _trendSeries = [];
        private Axis[] _trendXAxes = [];
        private Axis[] _trendYAxes = [];
        private string _trendLegend = string.Empty;
        private bool _hasTrend;

        private const string NoValue = "–";

        public IReadOnlyList<PeriodOption> Periods { get; } =
        [
            new("Heute", StatisticsPeriod.Today),
            new("Diese Woche", StatisticsPeriod.ThisWeek),
            new("Dieser Monat", StatisticsPeriod.ThisMonth),
            new("Dieses Jahr", StatisticsPeriod.ThisYear),
            new("Alles", StatisticsPeriod.All),
            new("Eigener Zeitraum", StatisticsPeriod.Custom),
        ];

        public PeriodOption SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (value == null)
                    return;

                SetProperty(ref _selectedPeriod, value);
                OnPropertyChanged(nameof(IsCustomPeriod));
                Recalculate();
            }
        }

        public bool IsCustomPeriod => SelectedPeriod.Period == StatisticsPeriod.Custom;

        public DateTime CustomFrom
        {
            get => _customFrom;
            set
            {
                SetProperty(ref _customFrom, value);
                Recalculate();
            }
        }

        public DateTime CustomTo
        {
            get => _customTo;
            set
            {
                SetProperty(ref _customTo, value);
                Recalculate();
            }
        }

        public IReadOnlyList<WeaponOption> WeaponOptions
        {
            get => _weaponOptions;
            set => SetProperty(ref _weaponOptions, value);
        }

        public WeaponOption SelectedWeapon
        {
            get => _selectedWeapon;
            set
            {
                // the picker clears the selection while its items are replaced
                if (value == null)
                    return;

                SetProperty(ref _selectedWeapon, value);
                Recalculate();
            }
        }

        public IReadOnlyList<CaliberOption> CaliberOptions
        {
            get => _caliberOptions;
            set => SetProperty(ref _caliberOptions, value);
        }

        public CaliberOption SelectedCaliber
        {
            get => _selectedCaliber;
            set
            {
                if (value == null)
                    return;

                SetProperty(ref _selectedCaliber, value);
                Recalculate();
            }
        }

        public string TotalShots { get => _totalShots; set => SetProperty(ref _totalShots, value); }
        public string TrainingShots { get => _trainingShots; set => SetProperty(ref _trainingShots, value); }
        public string CompetitionShots { get => _competitionShots; set => SetProperty(ref _competitionShots, value); }
        public string TrainingCount { get => _trainingCount; set => SetProperty(ref _trainingCount, value); }
        public string CompetitionCount { get => _competitionCount; set => SetProperty(ref _competitionCount, value); }
        public string TrainingPercentage { get => _trainingPercentage; set => SetProperty(ref _trainingPercentage, value); }
        public string CompetitionPercentage { get => _competitionPercentage; set => SetProperty(ref _competitionPercentage, value); }
        public bool HasData
        {
            get => _hasData;
            set => SetProperty(ref _hasData, value, onChanged: () => OnPropertyChanged(nameof(IsEmpty)));
        }
        public bool IsEmpty => !HasData;
        public ISeries[] ShotSeries { get => _shotSeries; set => SetProperty(ref _shotSeries, value); }
        public Axis[] XAxes { get => _xAxes; set => SetProperty(ref _xAxes, value); }

        public Axis[] YAxes { get => _yAxes; set => SetProperty(ref _yAxes, value); }

        // the legends follow the light or dark look
        public SolidColorPaint LegendTextPaint { get => _legendTextPaint; set => SetProperty(ref _legendTextPaint, value); }

        // results over time: one line per weapon, the x axis has one step per day with results
        public ISeries[] TrendSeries { get => _trendSeries; set => SetProperty(ref _trendSeries, value); }
        public Axis[] TrendXAxes { get => _trendXAxes; set => SetProperty(ref _trendXAxes, value); }
        public Axis[] TrendYAxes { get => _trendYAxes; set => SetProperty(ref _trendYAxes, value); }
        public string TrendLegend { get => _trendLegend; set => SetProperty(ref _trendLegend, value); }
        public bool HasTrend { get => _hasTrend; set => SetProperty(ref _hasTrend, value); }
        public string ChartTitle { get => _chartTitle; set => SetProperty(ref _chartTitle, value); }
        public IReadOnlyList<ShotsBucket> Days { get => _days; set => SetProperty(ref _days, value); }
        public IReadOnlyList<WeaponResultRow> WeaponResults { get => _weaponResults; set => SetProperty(ref _weaponResults, value); }

        public StatisticsViewModel(StatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
            Title = "Statistik";

            _selectedPeriod = Periods.First(n => n.Period == StatisticsPeriod.ThisMonth);
            _selectedWeapon = new WeaponOption("Alle Waffen", null);
            _weaponOptions = [_selectedWeapon];
            _selectedCaliber = new CaliberOption("Alle Kaliber", null);
            _caliberOptions = [_selectedCaliber];
        }

        // reloads the data each time the tab is shown, new trainings are included right away
        public async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                _isLoading = true;

                var (entries, weapons) = await _statisticsService.LoadAsync();
                _entries = entries;

                var weaponOptions = new List<WeaponOption> { new("Alle Waffen", null) };
                weaponOptions.AddRange(weapons
                    .OrderBy(n => n.IsRetired)
                    .ThenBy(n => n.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .Select(n => new WeaponOption(n.IsRetired ? $"{n.DisplayName} (ausgemustert)" : n.DisplayName, n.Id)));

                var caliberOptions = new List<CaliberOption> { new("Alle Kaliber", null) };
                caliberOptions.AddRange(weapons
                    .Where(n => !string.IsNullOrWhiteSpace(n.Caliber))
                    .GroupBy(n => StatisticsService.NormalizeName(n.Caliber))
                    .Select(n => new CaliberOption(n.First().Caliber!.Trim(), n.Key))
                    .OrderBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase));

                // keep the current filter, as long as the weapon or caliber still exists
                var weaponId = SelectedWeapon.WeaponId;
                var caliberKey = SelectedCaliber.CaliberKey;

                WeaponOptions = weaponOptions;
                CaliberOptions = caliberOptions;
                _selectedWeapon = weaponOptions.FirstOrDefault(n => n.WeaponId == weaponId) ?? weaponOptions[0];
                _selectedCaliber = caliberOptions.FirstOrDefault(n => n.CaliberKey == caliberKey) ?? caliberOptions[0];
                OnPropertyChanged(nameof(SelectedWeapon));
                OnPropertyChanged(nameof(SelectedCaliber));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _isLoading = false;
                IsBusy = false;
            }

            Recalculate();
        }

        private void Recalculate()
        {
            if (_isLoading)
                return;

            try
            {
                var (from, to) = StatisticsService.GetRange(SelectedPeriod.Period, DateTime.Today, CustomFrom, CustomTo);
                var result = _statisticsService.Calculate(_entries, new StatisticsFilter(from, to, SelectedWeapon.WeaponId, SelectedCaliber.CaliberKey));

                TotalShots = result.TotalShots.ToString();
                TrainingShots = result.TrainingShots.ToString();
                CompetitionShots = result.CompetitionShots.ToString();
                TrainingCount = result.TrainingCount.ToString();
                CompetitionCount = result.CompetitionCount.ToString();
                TrainingPercentage = FormatPercentage(result.TrainingPercentage);
                CompetitionPercentage = FormatPercentage(result.CompetitionPercentage);
                HasData = result.TrainingCount + result.CompetitionCount > 0;

                ChartTitle = result.BucketsPerMonth ? "Schüsse pro Monat" : "Schüsse pro Tag";
                CreateChart(result.Buckets);

                Days = result.Days;
                WeaponResults = result.Weapons
                    .Select(n => new WeaponResultRow(
                        n.IsRetired ? $"{n.WeaponName} (ausgemustert)" : n.WeaponName,
                        FormatResult(n.TrainingPercentage, n.TrainingCount),
                        FormatResult(n.CompetitionPercentage, n.CompetitionCount),
                        n.Shots))
                    .ToList();

                CreateTrendChart(result.Trends, result.BucketsPerMonth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        // the days of all weapons on one shared axis, each weapon connects its own days so a weapon
        // that was not shot on a day has no gap in its line
        private void CreateTrendChart(IReadOnlyList<WeaponTrend> trends, bool perMonth)
        {
            var days = trends.SelectMany(n => n.Points).Select(n => n.Day).Distinct().OrderBy(n => n).ToList();
            var index = days.Select((day, i) => (day, i)).ToDictionary(n => n.day, n => n.i);
            var labels = days.Select(n => n.ToString(perMonth ? "MM.yy" : "dd.MM.")).ToList();
            TrendLegend = perMonth
                ? "Ø Punkte in Prozent pro Monat, Trainings und Wettkämpfe"
                : "Ø Punkte in Prozent pro Tag, Trainings und Wettkämpfe";

            // the axis starts at the next lower ten below the weakest result, so the differences are visible
            double lowest = trends.SelectMany(n => n.Points).Select(n => n.Percentage).DefaultIfEmpty(0).Min();
            TrendYAxes = [ChartTheme.Apply(new Axis { MinLimit = Math.Max(0, Math.Floor((lowest - 5) / 10) * 10), MaxLimit = 100, TextSize = 11, Labeler = v => $"{v:0} %" })];

            TrendXAxes =
            [
                ChartTheme.Apply(new Axis
                {
                    Labeler = v => v >= 0 && v < labels.Count && Math.Abs(v - Math.Round(v)) < 0.01 ? labels[(int)Math.Round(v)] : string.Empty,
                    // about a dozen labels at most, a whole year of training days would be unreadable
                    MinStep = Math.Max(1, Math.Ceiling(days.Count / 12.0)),
                    // a single day would otherwise be squeezed to the left edge
                    MinLimit = -0.5,
                    MaxLimit = Math.Max(days.Count - 0.5, 0.5),
                    LabelsRotation = days.Count > 7 ? 60 : 0,
                    TextSize = 11,
                })
            ];

            // a weapon keeps its colour whatever is filtered: the colours follow the order of all weapons by shots
            var colorIndex = _entries
                .GroupBy(n => n.WeaponName)
                .OrderBy(n => n.Key == StatisticsService.WithoutWeaponName)
                .ThenByDescending(n => n.Sum(e => e.Shots))
                .Select((n, i) => (n.Key, i))
                .ToDictionary(n => n.Key, n => n.i);
            var colors = ChartTheme.Series;

            TrendSeries = trends
                .Select(trend =>
                {
                    var color = colors[colorIndex.GetValueOrDefault(trend.WeaponName) % colors.Length];
                    return (ISeries)new LineSeries<LiveChartsCore.Defaults.ObservablePoint>
                    {
                        Name = trend.WeaponName,
                        Values = trend.Points.Select(p => new LiveChartsCore.Defaults.ObservablePoint(index[p.Day], p.Percentage)).ToList(),
                        Stroke = new SolidColorPaint(color, 3),
                        GeometryStroke = new SolidColorPaint(color, 3),
                        GeometryFill = new SolidColorPaint(ChartTheme.CardBackground),
                        GeometrySize = 9,
                        Fill = null,
                        LineSmoothness = 0,
                        YToolTipLabelFormatter = p => $"{p.Model?.Y:0.0} %",
                    };
                })
                .ToArray();

            HasTrend = days.Count > 0;
        }

        private void CreateChart(IReadOnlyList<ShotsBucket> buckets)
        {
            LegendTextPaint = ChartTheme.LegendText;

            // the axis starts at 0, otherwise a single day looks like a big difference
            YAxes = [ChartTheme.Apply(new Axis { MinLimit = 0, TextSize = 11 })];

            XAxes =
            [
                ChartTheme.Apply(new Axis
                {
                    Labels = buckets.Select(n => n.Label).ToList(),
                    LabelsRotation = buckets.Count > 7 ? 60 : 0,
                    TextSize = 11,
                })
            ];

            ShotSeries =
            [
                new StackedColumnSeries<int>
                {
                    Name = "Training",
                    Values = buckets.Select(n => n.TrainingShots).ToList(),
                    Fill = new SolidColorPaint(ChartTheme.Accent),
                },
                new StackedColumnSeries<int>
                {
                    Name = "Wettkampf",
                    Values = buckets.Select(n => n.CompetitionShots).ToList(),
                    Fill = new SolidColorPaint(ChartTheme.Accent2),
                },
            ];
        }

        private static string FormatPercentage(double? percentage) =>
            percentage.HasValue ? $"{percentage.Value:0.0} %" : NoValue;

        private static string FormatResult(double? percentage, int count) =>
            count == 0 ? NoValue : $"{FormatPercentage(percentage)} ({count})";
    }
}
