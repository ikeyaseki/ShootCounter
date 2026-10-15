using System.Globalization;

namespace ShootCounter.Services
{
    public enum StatisticsPeriod
    {
        Today,
        ThisWeek,
        ThisMonth,
        ThisYear,
        All,
        Custom,
    }

    public enum ShootingKind
    {
        Training,
        Competition,
    }

    // one training or competition, reduced to what the statistics need
    public record StatisticsEntry(
        ShootingKind Kind,
        DateTime Date,
        Guid? WeaponId,
        string WeaponName,
        string? CaliberKey,
        int Shots,
        int Points,
        int MaxPoints,
        bool WeaponRetired);

    public record StatisticsFilter(DateTime? From, DateTime? To, Guid? WeaponId, string? CaliberKey);

    public record ShotsBucket(DateTime Start, string Label, int TrainingShots, int CompetitionShots)
    {
        public int TotalShots => TrainingShots + CompetitionShots;
    }

    public record WeaponResult(
        string WeaponName,
        bool IsRetired,
        int TrainingCount,
        double? TrainingPercentage,
        int CompetitionCount,
        double? CompetitionPercentage,
        int Shots);

    public record StatisticsResult(
        int TrainingShots,
        int CompetitionShots,
        int TrainingCount,
        int CompetitionCount,
        double? TrainingPercentage,
        double? CompetitionPercentage,
        IReadOnlyList<ShotsBucket> Buckets,
        bool BucketsPerMonth,
        IReadOnlyList<ShotsBucket> Days,
        IReadOnlyList<WeaponResult> Weapons)
    {
        public int TotalShots => TrainingShots + CompetitionShots;
    }

    public class StatisticsService
    {
        public const string WithoutWeaponName = "Ohne Zuordnung";

        // up to this many days the chart shows one column per day, otherwise one per month
        private const int MaxDaysPerDayBuckets = 62;

        // weekday names in german, independent of the language of the device
        private static readonly CultureInfo GermanCulture = new("de-CH");

        private readonly IDataStore<Shooting> _trainingDataStore;
        private readonly IDataStore<ShootingCompetition> _competitionDataStore;
        private readonly IDataStore<Weapon> _weaponDataStore;

        public StatisticsService(
            IDataStore<Shooting> trainingDataStore,
            IDataStore<ShootingCompetition> competitionDataStore,
            IDataStore<Weapon> weaponDataStore)
        {
            _trainingDataStore = trainingDataStore;
            _competitionDataStore = competitionDataStore;
            _weaponDataStore = weaponDataStore;
        }

        // names typed by hand, "9 mm Para" and "9mm para" or "Sig Sauer" and "SIG Sauer" belong together
        public static string? NormalizeName(string? name) =>
            string.IsNullOrWhiteSpace(name)
                ? null
                : string.Concat(name.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();

        // start (inclusive) and end (exclusive) of a period, weeks start on monday
        public static (DateTime? From, DateTime? To) GetRange(StatisticsPeriod period, DateTime today, DateTime customFrom, DateTime customTo)
        {
            today = today.Date;

            switch (period)
            {
                case StatisticsPeriod.Today:
                    return (today, today.AddDays(1));
                case StatisticsPeriod.ThisWeek:
                    var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                    return (monday, monday.AddDays(7));
                case StatisticsPeriod.ThisMonth:
                    var firstOfMonth = new DateTime(today.Year, today.Month, 1);
                    return (firstOfMonth, firstOfMonth.AddMonths(1));
                case StatisticsPeriod.ThisYear:
                    var firstOfYear = new DateTime(today.Year, 1, 1);
                    return (firstOfYear, firstOfYear.AddYears(1));
                case StatisticsPeriod.Custom:
                    var from = customFrom.Date <= customTo.Date ? customFrom.Date : customTo.Date;
                    var to = customFrom.Date <= customTo.Date ? customTo.Date : customFrom.Date;
                    return (from, to.AddDays(1));
                default:
                    return (null, null);
            }
        }

        public async Task<(IReadOnlyList<StatisticsEntry> Entries, IReadOnlyList<Weapon> Weapons)> LoadAsync()
        {
            var weapons = (await _weaponDataStore.GetItemsAsync(true)).ToList();
            var weaponsById = weapons.ToDictionary(n => n.Id);

            var entries = new List<StatisticsEntry>();

            foreach (var training in await _trainingDataStore.GetItemsAsync(true))
            {
                var maxValuation = training.TargetProperties?.Valuations?.Max() ?? 0;
                entries.Add(CreateEntry(ShootingKind.Training, training.StartTime, training.WeaponId, training.Weapon, training.Runs, maxValuation, weaponsById));
            }

            foreach (var competition in await _competitionDataStore.GetItemsAsync(true))
            {
                var maxValuation = competition.TargetProperties?.Valuations?.Max() ?? 0;
                entries.Add(CreateEntry(ShootingKind.Competition, competition.StartTime, competition.WeaponId, competition.Weapon, competition.Runs, maxValuation, weaponsById));
            }

            return (entries, weapons);
        }

        // percentage = points / (shots * highest valuation), so an aborted competition only counts the shots really shot
        private static StatisticsEntry CreateEntry(
            ShootingKind kind,
            DateTime date,
            Guid? weaponId,
            string? storedWeaponName,
            IEnumerable<Run>? runs,
            int maxValuation,
            Dictionary<Guid, Weapon> weaponsById)
        {
            var results = runs?.SelectMany(n => n.Results ?? []).ToList() ?? [];

            Weapon? weapon = weaponId.HasValue && weaponsById.TryGetValue(weaponId.Value, out var found) ? found : null;

            // only entries linked to a weapon count per weapon, older free text entries are "without assignment"
            var weaponName = weapon?.DisplayName ?? WithoutWeaponName;

            return new StatisticsEntry(
                kind,
                date,
                weapon?.Id,
                weaponName,
                NormalizeName(weapon?.Caliber),
                results.Count,
                results.Sum(),
                results.Count * maxValuation,
                weapon?.IsRetired ?? false);
        }

        public StatisticsResult Calculate(IEnumerable<StatisticsEntry> allEntries, StatisticsFilter filter)
        {
            var entries = allEntries
                .Where(n => filter.From == null || n.Date >= filter.From)
                .Where(n => filter.To == null || n.Date < filter.To)
                .Where(n => filter.WeaponId == null || n.WeaponId == filter.WeaponId)
                .Where(n => filter.CaliberKey == null || n.CaliberKey == filter.CaliberKey)
                .ToList();

            var trainings = entries.Where(n => n.Kind == ShootingKind.Training).ToList();
            var competitions = entries.Where(n => n.Kind == ShootingKind.Competition).ToList();

            var (buckets, perMonth) = CreateBuckets(entries, filter);

            var days = entries
                .GroupBy(n => n.Date.Date)
                .Select(n => CreateBucket(n.Key, n.Key.ToString("ddd dd.MM.yyyy", GermanCulture), n))
                .Where(n => n.TotalShots > 0)
                .OrderByDescending(n => n.Start)
                .ToList();

            var weapons = entries
                .GroupBy(n => n.WeaponName)
                .Select(n => new WeaponResult(
                    n.Key,
                    n.Any(e => e.WeaponRetired),
                    n.Count(e => e.Kind == ShootingKind.Training),
                    Percentage(n.Where(e => e.Kind == ShootingKind.Training)),
                    n.Count(e => e.Kind == ShootingKind.Competition),
                    Percentage(n.Where(e => e.Kind == ShootingKind.Competition)),
                    n.Sum(e => e.Shots)))
                .OrderBy(n => n.WeaponName == WithoutWeaponName)
                .ThenByDescending(n => n.Shots)
                .ToList();

            return new StatisticsResult(
                trainings.Sum(n => n.Shots),
                competitions.Sum(n => n.Shots),
                trainings.Count,
                competitions.Count,
                Percentage(trainings),
                Percentage(competitions),
                buckets,
                perMonth,
                days,
                weapons);
        }

        // weighted: all points divided by all possible points, a long training counts more than a short one
        private static double? Percentage(IEnumerable<StatisticsEntry> entries)
        {
            var list = entries.ToList();
            var maxPoints = list.Sum(n => n.MaxPoints);

            return maxPoints > 0 ? Math.Round(list.Sum(n => n.Points) * 100.0 / maxPoints, 1) : null;
        }

        private static ShotsBucket CreateBucket(DateTime start, string label, IEnumerable<StatisticsEntry> entries) =>
            new(start,
                label,
                entries.Where(n => n.Kind == ShootingKind.Training).Sum(n => n.Shots),
                entries.Where(n => n.Kind == ShootingKind.Competition).Sum(n => n.Shots));

        // one bucket per day or month over the whole range, empty days and months included
        private static (IReadOnlyList<ShotsBucket> Buckets, bool PerMonth) CreateBuckets(List<StatisticsEntry> entries, StatisticsFilter filter)
        {
            if (entries.Count == 0)
                return ([], false);

            var from = filter.From ?? entries.Min(n => n.Date).Date;
            var to = filter.To ?? entries.Max(n => n.Date).Date.AddDays(1);

            bool perMonth = (to - from).TotalDays > MaxDaysPerDayBuckets;
            var buckets = new List<ShotsBucket>();

            if (perMonth)
            {
                for (var month = new DateTime(from.Year, from.Month, 1); month < to; month = month.AddMonths(1))
                {
                    var next = month.AddMonths(1);
                    buckets.Add(CreateBucket(month, month.ToString("MM.yy"), entries.Where(n => n.Date >= month && n.Date < next)));
                }
            }
            else
            {
                for (var day = from; day < to; day = day.AddDays(1))
                {
                    var next = day.AddDays(1);
                    buckets.Add(CreateBucket(day, day.ToString("dd.MM."), entries.Where(n => n.Date >= day && n.Date < next)));
                }
            }

            return (buckets, perMonth);
        }
    }
}
