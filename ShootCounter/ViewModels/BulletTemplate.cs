namespace ShootCounter.ViewModels
{
    // one bullet on the scoring screen of a training or competition run
    public class BulletTemplate
    {
        public int BulletNumber { get; set; }
        public IEnumerable<ValuationTemplate>? Valuations { get; set; }
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
