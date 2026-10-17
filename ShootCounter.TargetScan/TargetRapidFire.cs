using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>
/// Geometry of the 25m rapid-fire target (ISSF, 50 x 52 cm), in millimetres, origin at the centre.
/// A black disc up to the 5 (radius 250 mm) with white ring lines: the 10 has a radius of 50 mm, every
/// further ring adds 40 mm. A white inner circle (radius 25 mm) marks the inner ten, it does not count
/// separately. Two white bars run from the edge to the 7 on the horizontal axis.
/// </summary>
public static class TargetRapidFire
{
    public const double BlackRadius = 250;
    public const double InnerTenRadius = 25;
    public const double CaliberMm = 9.0;

    /// <summary>Outer radius of the ring with the given value (5-10).</summary>
    public static double Radius(int value) => 50 + 40 * (10 - value);

    /// <summary>Value of a hit centred at distance d from the centre; touching a line counts the higher value.</summary>
    public static int Score(double d)
    {
        for (int value = 10; value >= 5; value--)
            if (d - CaliberMm / 2 <= Radius(value)) return value;
        return 0;
    }

    /// <summary>Distance in mm from the hole edge to the nearest ring line.</summary>
    public static double LineDistance(double d) =>
        Enumerable.Range(5, 6).Min(v => Math.Abs(d - CaliberMm / 2 - Radius(v)));

    /// <summary>True if (x, y) lies on the white print: ring lines, the inner ten, the bars or the digits.</summary>
    public static bool IsPrinted(double x, double y, double margin)
    {
        double d = Math.Sqrt(x * x + y * y);
        if (Math.Abs(d - InnerTenRadius) < 1.5 + margin) return true;
        if (Enumerable.Range(6, 5).Any(v => Math.Abs(d - Radius(v)) < 1.5 + margin)) return true;
        if (IsOnBar(x, y, margin)) return true;
        // digits 5-9 on the vertical axis, between the ring lines
        return Enumerable.Range(5, 5).Any(v => Math.Abs(x) < 6 + margin && Math.Abs(Math.Abs(y) - (Radius(v) - 20)) < 8 + margin);
    }

    public static bool IsOnBar(double x, double y, double margin) => Math.Abs(y) < 4 + margin && Math.Abs(x) > 120 - margin;

    static IEnumerable<SKPoint> Circle(double r, double step)
    {
        for (double t = 0; t < 2 * Math.PI; t += step / r)
            yield return new SKPoint((float)(r * Math.Cos(t)), (float)(r * Math.Sin(t)));
    }

    /// <summary>Points on the white ring lines and the inner ten, away from the bars and digits.</summary>
    public static List<SKPoint> LinePoints(double step) =>
        Enumerable.Range(6, 5).Select(v => Radius(v)).Append(InnerTenRadius)
            .SelectMany(r => Circle(r, step))
            .Where(p => !IsOnBar(p.X, p.Y, 6) && Math.Abs(p.X) > 12)
            .ToList();

    /// <summary>Points on the outline of the black disc.</summary>
    public static List<SKPoint> OutlinePoints(double step) =>
        Circle(BlackRadius, step).Where(p => !IsOnBar(p.X, p.Y, 6)).ToList();
}
