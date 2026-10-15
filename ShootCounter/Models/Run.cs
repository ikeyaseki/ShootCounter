using SQLite;
using System.Text.Json;

namespace ShootCounter.Models
{
    public class Run
    {
        private string? _resultsJson;
        private List<int>? _results;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public Guid ShootingId { get; set; }
        public DateTime Time { get; set; }
        public int RunNumber { get; set; }
        public int BulletNumber { get; set; }
        public string? ResultsJson
        {
            get => _resultsJson;
            set
            {
                _resultsJson = value;
                _results = null;
            }
        }
        // deserialized once and cached, instead of on every access
        [Ignore]
        public IEnumerable<int>? Results
        {
            get => _results ??= string.IsNullOrEmpty(_resultsJson)
                ? null
                : JsonSerializer.Deserialize<List<int>>(_resultsJson);
            set
            {
                ResultsJson = JsonSerializer.Serialize(value);
                _results = value?.ToList();
            }
        }
    }
}
