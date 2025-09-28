using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public class TargetProperties
    {
        [PrimaryKey]
        public Guid Id { get; set; } = Guid.NewGuid();
        public TargetType TargetType { get; set; }
        public string? Name { get; set; }
        public string? ValuationsJson { get; set; }
        [Ignore]
        public IEnumerable<int>? Valuations
        {
            get => string.IsNullOrEmpty(ValuationsJson)
                ? null
                : JsonSerializer.Deserialize<IEnumerable<int>>(ValuationsJson);
            set => ValuationsJson = JsonSerializer.Serialize(value);
        }
    }
}
