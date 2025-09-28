using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public class Run
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public Guid ShootingId { get; set; }
        public DateTime Time { get; set; }
        public int RunNumber { get; set; }
        public int BulletNumber { get; set; }
        public string? ResultsJson { get; set; }
        [Ignore]
        public IEnumerable<int>? Results
        {
            get => string.IsNullOrEmpty(ResultsJson)
                ? null
                : JsonSerializer.Deserialize<IEnumerable<int>>(ResultsJson);
            set => ResultsJson = JsonSerializer.Serialize(value);
        }
    }
}
