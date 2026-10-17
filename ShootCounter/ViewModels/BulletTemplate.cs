namespace ShootCounter.ViewModels
{
    // one bullet on the scoring screen of a training or competition run
    public class BulletTemplate
    {
        public int BulletNumber { get; set; }
        public IEnumerable<ValuationTemplate>? Valuations { get; set; }

        // bullets of a run prefilled with scanned scores (highest first),
        // bullets without a score stay open unless missing ones count as 0
        public static List<BulletTemplate> FromScores(IReadOnlyList<int> scores, int bulletCount, TargetProperties? targetProperties, bool missingAsZero)
        {
            var bullets = new List<BulletTemplate>();

            for (int bullet = 0; bullet < bulletCount; bullet++)
            {
                int? score = bullet < scores.Count ? scores[bullet] : missingAsZero ? 0 : null;
                var valuations = ValuationTemplate.CreateList(targetProperties);
                var valuation = valuations.FirstOrDefault(n => n.Value == score);
                if (valuation != null)
                    valuation.Hit = true;

                bullets.Add(new BulletTemplate { BulletNumber = bullet + 1, Valuations = valuations });
            }

            return bullets;
        }
    }

    public class ValuationTemplate
    {
        public int Value { get; set; }
        public bool Hit { get; set; }

        public static List<ValuationTemplate> CreateList(TargetProperties? targetProperties)
        {
            if (targetProperties?.Valuations == null)
                throw new Exception("Valuations not found");

            return targetProperties.Valuations
                .Select(n => new ValuationTemplate { Value = n, Hit = false })
                .ToList();
        }
    }
}
