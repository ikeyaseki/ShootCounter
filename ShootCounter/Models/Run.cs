using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Models
{
    public class Run
    {
        public Guid Id { get; set; }
        public DateTime Time { get; set; }
        public int RunNumber { get; set; }
        public int BulletNumber { get; set; }
        public IEnumerable<int>? Results { get; set; }
    }
}
