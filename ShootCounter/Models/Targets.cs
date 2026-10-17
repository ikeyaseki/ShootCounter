using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public static class Targets
    {
        public static IEnumerable<TargetProperties> TargetProperties { get; set; } = new List<TargetProperties>
            {
                new TargetProperties() { TargetType = TargetType.OrderlyRapidFire, Name = "Ordonnanz", Valuations = [0, 6, 7, 8, 9, 10] },
                new TargetProperties() { TargetType = TargetType.PrecisionTargetP4, Name = "P4", Valuations = [0, 1, 2, 3, 4] },
                new TargetProperties() { TargetType = TargetType.PrecisionTargetP10, Name = "P10", Valuations = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10] },
                new TargetProperties() { TargetType = TargetType.CombinationTargetB5, Name = "B5", Valuations = [0, 1, 2, 3, 4, 5]},
                new TargetProperties() { TargetType = TargetType.CombinationTargetB10, Name = "B10", Valuations = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10] },
                new TargetProperties() { TargetType = TargetType.Morgarten, Name = "Morgarten", Valuations = [0, 1, 2, 3, 4, 5] },
            };
    }
}
