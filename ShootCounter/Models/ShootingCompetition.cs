using SQLite;

namespace ShootCounter.Models
{
    public class ShootingCompetition
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [Ignore]
        public TimeSpan Duration
        {
            get => EndTime - StartTime;
        }
        public string? Location { get; set; }
        public string? Weapon { get; set; }
        public CompetitionType CompetitionType { get; set; }
        [Ignore]
        public TargetProperties? TargetProperties { get; set; }
        [Ignore]
        public CompetitionProperties? CompetitionProperties { get; set; }
        [Ignore]
        public IEnumerable<Run>? Runs { get; set; }
        [Ignore]
        public int Result { get; set; }

        public ShootingCompetition()
        {
            Id = Guid.NewGuid();
        }
    }
}
