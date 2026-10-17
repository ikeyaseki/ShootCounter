using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>
/// Geometry of the Swiss 25m ordnance rapid-fire target (black, rings 6-10), in millimetres.
/// Origin at the target centre, y pointing down.
/// Ring outlines are stadiums (straight vertical sides, semicircular ends). The 10 is 100 x 150 mm,
/// every further ring adds 50 mm to the half-width and 75 mm to the half-height
/// (measured from a reference image of the 760 x 450 mm target).
/// </summary>
public static class Target25m
{
    public const double BlackWidth = 450, BlackHeight = 760;

    /// <summary>Outer boundary of ring 10, 9, 8, 7, 6 (half-width a, half-height b).</summary>
    public static readonly (int value, double a, double b)[] Rings =
        Enumerable.Range(1, 5).Select(k => (11 - k, 50.0 * k, 75.0 * k)).ToArray();

    public const double CaliberMm = 9.0;

    /// <summary>Signed distance of p to the stadium outline (negative = inside).</summary>
    public static double StadiumDist(double x, double y, double a, double b)
    {
        double c = Math.Max(0, b - a), ay = Math.Abs(y);
        return ay <= c ? Math.Abs(x) - a : Math.Sqrt(x * x + (ay - c) * (ay - c)) - a;
    }

    public static IEnumerable<SKPoint> StadiumOutline(double a, double b, double step)
    {
        double c = Math.Max(0, b - a);
        for (double y = -c; y < c; y += step) yield return new((float)a, (float)y);
        for (double t = 0; t < Math.PI; t += step / a) yield return new((float)(a * Math.Cos(t)), (float)(c + a * Math.Sin(t)));
        for (double y = c; y > -c; y -= step) yield return new((float)-a, (float)y);
        for (double t = Math.PI; t < 2 * Math.PI; t += step / a) yield return new((float)(a * Math.Cos(t)), (float)(-c + a * Math.Sin(t)));
    }

    static bool OnBlack(SKPoint p) => Math.Abs(p.X) < BlackWidth / 2 - 4 && Math.Abs(p.Y) < BlackHeight / 2 - 4;

    /// <summary>Points on all printed ring lines that lie on the black area.</summary>
    public static List<SKPoint> LinePoints(double step, (int value, double a, double b)[]? rings = null) =>
        (rings ?? Rings).SelectMany(r => StadiumOutline(r.a, r.b, step)).Where(OnBlack).ToList();

    /// <summary>
    /// Points halfway between neighbouring ring lines, away from the printed digits.
    /// A correct fit has (almost) no line pixels there; a fit that is off by one ring has many.
    /// </summary>
    public static List<SKPoint> BandPoints(double step) =>
        Enumerable.Range(0, Rings.Length)
            .SelectMany(k => StadiumOutline(50.0 * (k + 0.5), 75.0 * (k + 0.5), step))
            .Where(p => OnBlack(p) && Math.Abs(p.X) > 30 && Math.Abs(p.Y) > 35)
            .ToList();

    /// <summary>
    /// Digit centres, midway between neighbouring ring lines: 9-6 on the vertical axis,
    /// 9-7 on the horizontal axis (both sides).
    /// </summary>
    public static IEnumerable<SKPoint> DigitCentres((int value, double a, double b)[] rings)
    {
        for (int k = 1; k < rings.Length; k++)
        {
            float y = (float)((rings[k - 1].b + rings[k].b) / 2);
            yield return new(0, y);
            yield return new(0, -y);
            if (k < rings.Length - 1)
            {
                float x = (float)((rings[k - 1].a + rings[k].a) / 2);
                yield return new(x, 0);
                yield return new(-x, 0);
            }
        }
    }

    /// <summary>True if (x, y) lies on a printed ring line or digit (with some margin).</summary>
    public static bool IsPrinted(double x, double y, (int value, double a, double b)[] rings)
    {
        foreach (var r in rings)
            if (Math.Abs(StadiumDist(x, y, r.a, r.b)) < 3) return true;
        foreach (var d in DigitCentres(rings))
            if (Math.Abs(x - d.X) < 18 && Math.Abs(y - d.Y) < 28) return true;
        return false;
    }

    /// <summary>
    /// Score of a hit centred at (x, y): the highest ring the hole touches
    /// (a hole touching the ring line counts the higher value), 0 if outside ring 6.
    /// </summary>
    public static int Score(double x, double y, (int value, double a, double b)[]? rings = null)
    {
        foreach (var ring in rings ?? Rings)
            if (StadiumDist(x, y, ring.a, ring.b) <= CaliberMm / 2) return ring.value;
        return 0;
    }

    /// <summary>
    /// Distance in mm from the hole edge to the nearest ring line that decides the score
    /// (the line of its own ring or of the next higher one). Small values are line shots.
    /// </summary>
    public static double LineDistance(double x, double y, (int value, double a, double b)[] rings) =>
        rings.Min(r => Math.Abs(StadiumDist(x, y, r.a, r.b) - CaliberMm / 2));
}
