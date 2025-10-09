namespace ShootCounter.Helpers
{
    class CalculateShooting
    {
        public Task<ShootingCompetition> CalculateCompetition(ShootingCompetition competition)
        {
            competition.Result = 0;

            if (competition.Runs == null)
                return Task.FromResult(competition);

            foreach (var run in competition.Runs)
            {
                if (run.Results == null)
                    throw new Exception("Run results can not be null here");

                competition.Result += run.Results.Sum();
            }

            return Task.FromResult(competition);
        }

        public Task<Shooting> CalculateTraining(Shooting shooting)
        {
            shooting.TotalBullets = 0;
            shooting.MaxPoints = 0;
            shooting.TotalPoints = 0;
            shooting.TotalPercentage = 0;
            shooting.BulletsMaxPoints = 0;
            shooting.BulletsZeroPoints = 0;

            if (shooting.Runs == null)
                return Task.FromResult(shooting);

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
