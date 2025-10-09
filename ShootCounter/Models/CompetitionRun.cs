
namespace ShootCounter.Models
{
    public class CompetitionRun
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int RunNumber { get; set; }
        public int NumberOfBullets { get; set; }
        public int? Duration { get; set; }
        public bool Einzelgezeigt { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
