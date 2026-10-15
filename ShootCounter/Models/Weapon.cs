using SQLite;

namespace ShootCounter.Models
{
    public class Weapon
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? Caliber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? Notes { get; set; }

        // name shown in lists and pickers, also stored in trainings and competitions as text
        [Ignore]
        public string DisplayName
        {
            get
            {
                var name = $"{Manufacturer} {Model}".Trim();
                return string.IsNullOrWhiteSpace(Caliber) ? name : $"{name} ({Caliber})";
            }
        }
    }
}
