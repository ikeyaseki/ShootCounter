using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.Services
{
    class CalculateShooting
    {
        public Task<Shooting> Calculate(Shooting shooting)
        {
            if (shooting.Runs == null)
                throw new Exception("Shooting can not be null here");

            shooting.TotalBullets = 0;
            shooting.MaxPoints = 0;
            shooting.TotalPoints = 0;
            shooting.TotalPercentage = 0;
            shooting.BulletsMaxPoints = 0;
            shooting.BulletsZeroPoints = 0;

            if (shooting?.TargetProperties?.Valuations == null)
                throw new Exception("Target valuations can not be null here");

            var maxTarget = shooting.TargetProperties.Valuations.Max();

            foreach (var run in shooting.Runs)
            {
                if (run.Results == null)
                    throw new Exception("Run results can not be null here");

                foreach (var bullet in run.Results)
                {
                    shooting.TotalBullets++;
                    shooting.TotalPoints += bullet;

                    if (bullet == 0)
                        shooting.BulletsZeroPoints++;

                    if (bullet == maxTarget)
                        shooting.BulletsMaxPoints++;
                }
            }

            shooting.MaxPoints = shooting.TotalBullets * maxTarget;

            shooting.TotalPercentage = Math.Round(shooting.TotalPoints / (double)shooting.MaxPoints * 100);

            return Task.FromResult(shooting);
        }
    }
}
