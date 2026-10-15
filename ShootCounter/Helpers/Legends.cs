namespace ShootCounter.Helpers
{
    // one line of a legend: the icons as they appear in the card and their meaning
    public record LegendItem(string Text, params string[] Icons);

    // legends of the icons used in the training and competition overviews
    public static class Legends
    {
        public static IReadOnlyList<LegendItem> Training { get; } =
        [
            new("Dauer", IconFont.ClockTimeNineOutline),
            new("Distanz", IconFont.MapMarkerDistance),
            new("Scheibe", IconFont.Bullseye),
            new("Waffe", IconFont.Pistol),
            new("Schüsse pro Passe", IconFont.Ammunition),
            new("Geschossene Schüsse total", IconFont.ArrowAll, IconFont.Ammunition),
            new("Maximal mögliche Punkte", IconFont.ArrowAll, IconFont.GiftOutline),
            new("Erreichte Punkte in Prozent", IconFont.Account, IconFont.PercentBoxOutline),
            new("Erreichte Punkte", IconFont.Account, IconFont.GiftOutline),
            new("Schüsse mit 0 Punkten", IconFont.ArrowDownBoldOutline, IconFont.Ammunition),
            new("Schüsse mit der höchsten Wertung", IconFont.ArrowUpBoldOutline, IconFont.Ammunition),
        ];

        public static IReadOnlyList<LegendItem> Competition { get; } =
        [
            new("Dauer", IconFont.ClockTimeNineOutline),
            new("Waffe", IconFont.Pistol),
            new("Maximal mögliche Punkte", IconFont.ArrowAll, IconFont.GiftOutline),
            new("Erreichte Punkte", IconFont.Account, IconFont.GiftOutline),
        ];
    }
}
