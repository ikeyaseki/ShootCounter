using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public enum TargetType
    {
        Unknown = 0,
        OrderlyRapidFire = 1,
        // stored as number in the database: existing values must not change
        PrecisionTargetP10 = 2,
        CombinationTargetB5 = 3,
        CombinationTargetB10 = 4,
        Morgarten = 5,
        PrecisionTargetP4 = 6,
    }
}
