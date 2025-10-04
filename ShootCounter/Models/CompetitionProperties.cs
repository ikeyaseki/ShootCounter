
namespace ShootCounter.Models
{
    public class CompetitionProperties
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public CompetitionType CompetitionType { get; set; }
        public DistanceType DistanceType { get; set; }
        public TargetType TargetType { get; set; }
        public string? Name { get; set; }
        public IEnumerable<CompetitionRun>? CompetitionRuns { get; set; }
        public bool TrialShots { get; set; }

        public int MaxPoints { get; set; }
        public int? MinPoints { get; set; }
        public int? MaxZeros { get; set; }
        public int? KartePoints { get; set; }
        public int? KranzPoints { get; set; }
    }
}
