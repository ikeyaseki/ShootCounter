using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public class TargetProperties
    {
        public Guid Id { get; } = Guid.NewGuid();
        public TargetType TargetType { get; set; }
        public string? Name { get; set; }
        public IEnumerable<int>? Valuations { get; set; }
    }
}
