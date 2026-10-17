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
                new TargetProperties() { TargetType = TargetType.OrderlyRapidFire, Name = "Ordonnanz", Valuations = [0, 6, 7, 8, 9, 10], Distance = DistanceType.Distance25m, Image = "target_ordnance.png" },
                new TargetProperties() { TargetType = TargetType.RapidFire, Name = "Schnellfeuer", Valuations = [0, 5, 6, 7, 8, 9, 10], Distance = DistanceType.Distance25m, Image = "target_rapidfire.png" },
                new TargetProperties() { TargetType = TargetType.PrecisionTargetPP10, Name = "PP10", Valuations = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10], Distance = DistanceType.Distance25m, Image = "target_pp10.png" },
                new TargetProperties() { TargetType = TargetType.PrecisionTargetP4, Name = "P4", Valuations = [0, 1, 2, 3, 4], Distance = DistanceType.Distance50m, Image = "target_precision.png" },
                new TargetProperties() { TargetType = TargetType.PrecisionTargetP10, Name = "P10", Valuations = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10], Distance = DistanceType.Distance50m, Image = "target_precision.png" },
                new TargetProperties() { TargetType = TargetType.CombinationTargetB5, Name = "B5", Valuations = [0, 1, 2, 3, 4, 5], Distance = DistanceType.Distance50m, Image = "target_combination.png" },
                new TargetProperties() { TargetType = TargetType.CombinationTargetB10, Name = "B10", Valuations = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10], Distance = DistanceType.Distance50m, Image = "target_combination.png" },
                new TargetProperties() { TargetType = TargetType.Morgarten, Name = "Morgarten", Valuations = [0, 1, 2, 3, 4, 5], Distance = DistanceType.Distance50m, Image = "target_morgarten.png" },
            };
    }
}
