using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
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
        private string _chartTitle = string.Empty;
        private IReadOnlyList<ShotsBucket> _days = [];
        private IReadOnlyList<WeaponResultRow> _weaponResults = [];

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

        // the axis starts at 0, otherwise a single day looks like a big difference
        public Axis[] YAxes { get; } = [new Axis { MinLimit = 0, TextSize = 11 }];
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void CreateChart(IReadOnlyList<ShotsBucket> buckets)
        {
            XAxes =
            [
                new Axis
                {
                    Labels = buckets.Select(n => n.Label).ToList(),
                    LabelsRotation = buckets.Count > 7 ? 60 : 0,
                    TextSize = 11,
                }
            ];

            ShotSeries =
            [
                new StackedColumnSeries<int>
                {
                    Name = "Training",
                    Values = buckets.Select(n => n.TrainingShots).ToList(),
                    Fill = new SolidColorPaint(SKColors.BlueViolet),
                },
                new StackedColumnSeries<int>
                {
                    Name = "Wettkampf",
                    Values = buckets.Select(n => n.CompetitionShots).ToList(),
                    Fill = new SolidColorPaint(SKColors.Orange),
                },
            ];
        }

        private static string FormatPercentage(double? percentage) =>
            percentage.HasValue ? $"{percentage.Value:0.0} %" : NoValue;

        private static string FormatResult(double? percentage, int count) =>
            count == 0 ? NoValue : $"{FormatPercentage(percentage)} ({count})";
    }
}
