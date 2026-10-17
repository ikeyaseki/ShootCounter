using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ShootCounter.Helpers
{
    // colours of the charts and drawings matching the light or dark look of the app (see Colors.xaml),
    // taken when a chart is created
    public static class ChartTheme
    {
        public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

        public static SKColor Text => IsDark ? SKColor.Parse("#A0A0A8") : SKColor.Parse("#6E6E6E");
        public static SKColor PrimaryText => IsDark ? SKColor.Parse("#ECECEC") : SKColor.Parse("#212121");
        public static SKColor GridLine => IsDark ? SKColor.Parse("#303036") : SKColor.Parse("#E2E0EA");
        public static SKColor CardBackground => IsDark ? SKColor.Parse("#1E1E22") : SKColors.White;
        public static SKColor PageBackground => IsDark ? SKColor.Parse("#121212") : SKColor.Parse("#F2F1F6");

        // trainings and the first series
        public static SKColor Accent => IsDark ? SKColor.Parse("#AC99EA") : SKColor.Parse("#512BD4");
        // competitions
        public static SKColor Accent2 => IsDark ? SKColor.Parse("#FFB74D") : SKColor.Parse("#F57C00");

        // colours of several series, e.g. one line per weapon
        public static SKColor[] Series => IsDark
            ? [SKColor.Parse("#AC99EA"), SKColor.Parse("#FFB74D"), SKColor.Parse("#66BB6A"), SKColor.Parse("#EF5350"), SKColor.Parse("#42A5F5"), SKColor.Parse("#FFD54F"), SKColor.Parse("#BDBDBD")]
            : [SKColor.Parse("#512BD4"), SKColor.Parse("#F57C00"), SKColor.Parse("#2E7D32"), SKColor.Parse("#C62828"), SKColor.Parse("#1E88E5"), SKColor.Parse("#B8860B"), SKColor.Parse("#757575")];

        public static SolidColorPaint LegendText => new(PrimaryText);

        // labels and grid lines of an axis in the colours of the theme
        public static Axis Apply(Axis axis)
        {
            axis.LabelsPaint = new SolidColorPaint(Text);
            axis.NamePaint = new SolidColorPaint(Text);
            axis.SeparatorsPaint = new SolidColorPaint(GridLine, 1);
            return axis;
        }
    }
}
