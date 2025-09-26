using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public class Shooting
    {
        public Guid Id{ get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public DistanceType DistanceType {  get; set; }
        public TargetType TargetType { get; set; }
        public string? Weapon {  get; set; }
    }
}
