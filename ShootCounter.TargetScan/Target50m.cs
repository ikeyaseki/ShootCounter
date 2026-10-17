using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>
/// Geometry of the Swiss 50m P10/P4 pistol target (1 x 1 m), in millimetres, origin at the centre.
/// Concentric circles every 50 mm: the 10 has a radius of 50 mm, the black disc is the 9 (radius 100 mm),
/// the 1 has a radius of 500 mm (measured from a reference image).
/// The P4 values are printed on the same target: 9-10 count 4, 8 counts 3, 5-7 count 2, 1-4 count 1.
/// </summary>
public static class Target50m
{
    public const double Size = 1000;
    public const double BlackRadius = 100;
    public const double CaliberMm = 9.0;

    /// <summary>Outer radius of the ring with the given P10 value (1-10).</summary>
    public static double Radius(int value) => 50.0 * (11 - value);

    /// <summary>P10 value of a hit centred at distance d from the centre; touching a line counts the higher value.</summary>
    public static int ScoreP10(double d, double scale = 1)
    {
        for (int value = 10; value >= 1; value--)
            if (d - CaliberMm / 2 <= Radius(value) * scale) return value;
        return 0;
    }

    public static int P10ToP4(int p10) => p10 switch
    {
        >= 9 => 4,
        8 => 3,
        >= 5 => 2,
        >= 1 => 1,
        _ => 0,
    };

    /// <summary>Distance in mm from the hole edge to the nearest ring line (small values are line shots).</summary>
    public static double LineDistance(double d, double scale = 1) =>
        Enumerable.Range(1, 10).Min(v => Math.Abs(d - CaliberMm / 2 - Radius(v) * scale));

    /// <summary>Like <see cref="LineDistance"/>, but only for the lines that change the P4 value (outer lines of 9, 8, 5 and 1).</summary>
    public static double LineDistanceP4(double d) =>
        new[] { 9, 8, 5, 1 }.Min(v => Math.Abs(d - CaliberMm / 2 - Radius(v)));

    static IEnumerable<SKPoint> Circle(double r, double step)
    {
        for (double t = 0; t < 2 * Math.PI; t += step / r)
            yield return new SKPoint((float)(r * Math.Cos(t)), (float)(r * Math.Sin(t)));
    }

    /// <summary>Points on the printed dark ring lines (rings 8 to 1; the 9 is the edge of the black disc).</summary>
    public static List<SKPoint> LinePoints(double step) =>
        Enumerable.Range(1, 8).SelectMany(v => Circle(Radius(v), step)).ToList();

    /// <summary>Points halfway between neighbouring ring lines.</summary>
    public static List<SKPoint> BandPoints(double step) =>
        Enumerable.Range(1, 7).SelectMany(v => Circle(Radius(v) - 25, step)).ToList();
}
