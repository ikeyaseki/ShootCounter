using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ShootCounter.Helpers
{
    // column chart of the bullets per run, used by the training and competition details
    public static class RunChart
    {
        public static bool TryCreate(IEnumerable<Run>? runs, out Axis[] xAxes, out IEnumerable<ISeries> series)
        {
            xAxes = [];
            series = [];

            // a shooting stopped before the first run has no runs, Max() would throw on an empty list
            var runResults = runs?.Select(n => n.Results?.ToList() ?? []).ToList() ?? [];
            if (runResults.Count == 0)
                return false;

            xAxes =
            [
                ChartTheme.Apply(new Axis
                {
                    Name = "Passen",
                    Labels = Enumerable.Range(1, runResults.Count).Select(x => x.ToString()).ToList(),
                })
            ];

            var maxLength = runResults.Max(n => n.Count);

            series = Enumerable.Range(0, maxLength + 1)
                .Select(i => new ColumnSeries<int>
                {
                    Name = i >= maxLength ? "Platzhalter" : $"Schuss {i + 1}",
                    Values = runResults.Select(results => i < results.Count ? results[i] : 0).ToList(),
                    Fill = new SolidColorPaint(ChartTheme.Accent),
                })
                .ToList<ISeries>();

            return true;
        }
    }
}
