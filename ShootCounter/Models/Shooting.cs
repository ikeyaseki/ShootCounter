using SQLite;

namespace ShootCounter.Models
{
    public class Shooting
    {
        [PrimaryKey]
        public Guid Id{ get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [Ignore]
        public TimeSpan Duration
        {
            get => EndTime - StartTime;
        }

        public string? Location { get; set; }
        public DistanceType DistanceType {  get; set; }
        public TargetType TargetType { get; set; }
        public string? Weapon {  get; set; }
        public int? BulletsPerRun { get; set; }
        [Ignore]
        public TargetProperties? TargetProperties { get; set; }
        [Ignore]
        public IEnumerable<Run>? Runs { get; set; }
        [Ignore]
        public int TotalBullets { get; set; }
        public int MaxPoints { get; set; }
        public int TotalPoints { get; set; }
        public double TotalPercentage { get; set; }
        public int BulletsMaxPoints { get; set; }
        public int BulletsZeroPoints { get; set; }

        public Shooting()
        {
            Id = Guid.NewGuid();
        }
    }
}
