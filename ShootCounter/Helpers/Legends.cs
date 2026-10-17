namespace ShootCounter.Helpers
{
    // one line of a legend: the icons as they appear in the card and their meaning
    public record LegendItem(string Text, params string[] Icons);

    // legends of the icons used in the training and competition overviews
    public static class Legends
    {
        public static IReadOnlyList<LegendItem> Training { get; } =
        [
            new("Rechts oben: erreichte Punkte in Prozent, darunter erreichte und maximal mögliche Punkte"),
            new("Dauer", IconFont.ClockTimeNineOutline),
            new("Distanz", IconFont.MapMarkerDistance),
            new("Scheibe", IconFont.Bullseye),
            new("Schüsse pro Passe", IconFont.Ammunition),
            new("Waffe", IconFont.Pistol),
            new("Geschossene Schüsse total", IconFont.ArrowAll, IconFont.Ammunition),
            new("Schüsse mit der höchsten Wertung", IconFont.ArrowUpBoldOutline, IconFont.Ammunition),
            new("Schüsse mit 0 Punkten", IconFont.ArrowDownBoldOutline, IconFont.Ammunition),
        ];

        public static IReadOnlyList<LegendItem> Competition { get; } =
        [
            new("Rechts oben: erreichte Punkte, darunter die maximal möglichen Punkte"),
            new("Dauer", IconFont.ClockTimeNineOutline),
            new("Waffe", IconFont.Pistol),
        ];
    }
}
