using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public class Targets
    {
        public IEnumerable<TargetProperties>? TargetProperties { get; set; }

        public Targets()
        {
            List<TargetProperties> targetProperties = new List<TargetProperties>
            {
                new TargetProperties() { TargetType = TargetType.OrderlyRapidFire, Name = "Orderly Rapid Fire", Valuations = [0, 6, 7, 8, 9, 10] },
                new TargetProperties() { TargetType = TargetType.PrecisionTarget, Name = "Precision Target", Valuations = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10] },
            };

            TargetProperties = targetProperties;
        }
    }
}
