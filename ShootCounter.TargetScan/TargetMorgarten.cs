using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>
/// Geometry of the Morgarten target (50m, green figure on a 45 x 100 cm sheet), in millimetres,
/// origin at the centre of the ring ellipses, y pointing down. The figure is usually cut out, so
/// outside of it there is no paper at all. White ellipses mark the values 5 to 2, the rest of the
/// figure counts 1 and a hit outside the figure 0.
/// Measured on a reference image of about 1.25 mm per pixel, so the shapes are accurate to a few mm.
/// </summary>
public static class TargetMorgarten
{
    public const double CaliberMm = 9.0;

    /// <summary>Outer ellipse of the value 5, 4, 3 and 2 (half-width a, half-height b).</summary>
    public static readonly (int value, double a, double b)[] Rings =
    [
        (5, 47.5, 77),
        (4, 125.5, 184),
        (3, 209, 292),
        (2, 287, 388),
    ];

    // body and head of the figure as rectangles (left, top, right, bottom)
    static readonly (double l, double t, double r, double b) Body = (-222.5, -375, 222.5, 410);
    static readonly (double l, double t, double r, double b) Head = (-101, -579, 101, -375);

    /// <summary>The whole figure, for display.</summary>
    public static readonly SKRect Bounds = new(-230, -585, 230, 415);

    /// <summary>Signed distance to the outline of the figure (negative inside).</summary>
    public static double FigureDistance(double x, double y) => Math.Min(BoxDistance(Body, x, y), BoxDistance(Head, x, y));

    /// <summary>Approximate signed distance (mm) to an ellipse, negative inside.</summary>
    public static double EllipseDistance(double x, double y, double a, double b)
    {
        double rho = Math.Sqrt(x * x / (a * a) + y * y / (b * b));
        if (rho < 1e-9) return -Math.Min(a, b);
        double len = Math.Sqrt(x * x + y * y);
        return len * (1 - 1 / rho);
    }

    /// <summary>Value of a hit centred at (x, y); a hole touching a line counts the higher value.</summary>
    public static int Score(double x, double y)
    {
        double r = CaliberMm / 2;
        foreach (var ring in Rings)
            if (EllipseDistance(x, y, ring.a, ring.b) <= r) return ring.value;
        return FigureDistance(x, y) <= r ? 1 : 0;
    }

    /// <summary>Distance in mm from the hole edge to the nearest line that changes the value.</summary>
    public static double LineDistance(double x, double y)
    {
        double r = CaliberMm / 2;
        return Rings.Select(g => Math.Abs(EllipseDistance(x, y, g.a, g.b) - r))
            .Append(Math.Abs(FigureDistance(x, y) - r)).Min();
    }

    static IEnumerable<SKPoint> Ellipse(double a, double b, double step)
    {
        for (double t = 0; t < 2 * Math.PI; t += step / Math.Max(a, b))
            yield return new SKPoint((float)(a * Math.Cos(t)), (float)(b * Math.Sin(t)));
    }

    /// <summary>Points on the white ring lines that lie on the figure (ring 2 runs partly outside it).</summary>
    public static List<SKPoint> RingPoints(double step) =>
        Rings.SelectMany(g => Ellipse(g.a, g.b, step)).Where(p => FigureDistance(p.X, p.Y) < -4).ToList();

    /// <summary>Points on the outline of the figure.</summary>
    public static List<SKPoint> OutlinePoints(double step)
    {
        var pts = new List<SKPoint>();
        void Segment(double x0, double y0, double x1, double y1)
        {
            double len = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            for (double s = 0; s < len; s += step)
                pts.Add(new SKPoint((float)(x0 + (x1 - x0) * s / len), (float)(y0 + (y1 - y0) * s / len)));
        }
        Segment(Head.l, Head.t, Head.r, Head.t);
        Segment(Head.r, Head.t, Head.r, Body.t);
        Segment(Head.r, Body.t, Body.r, Body.t);
        Segment(Body.r, Body.t, Body.r, Body.b);
        Segment(Body.r, Body.b, Body.l, Body.b);
        Segment(Body.l, Body.b, Body.l, Body.t);
        Segment(Body.l, Body.t, Head.l, Body.t);
        Segment(Head.l, Body.t, Head.l, Head.t);
        return pts;
    }

    /// <summary>
    /// Centres of the printed white digits: on the vertical axis between the ellipses and on the horizontal
    /// axis (values 3 and 4 on both sides), plus the 1s in the head and the lower corners, and the small
    /// print at the bottom (name and number of the target), with their half-sizes.
    /// </summary>
    static readonly (SKPoint centre, float hx, float hy)[] Prints =
    [
        .. new SKPoint[]
        {
            new(0, -470), new(0, -342), new(0, -238), new(0, -132), new(0, 0), new(0, 132), new(0, 238), new(0, 342),
            new(-165, 0), new(165, 0), new(-86, 0), new(87, 0), new(-187, 346), new(183, 346),
        }.Select(p => (p, 9f, 12f)),
        (new SKPoint(-155, 377), 45f, 10f),
        (new SKPoint(110, 377), 30f, 10f),
    ];

    public static bool IsNearPrint(double x, double y, double margin) =>
        Prints.Any(d => Math.Abs(x - d.centre.X) < d.hx + margin && Math.Abs(y - d.centre.Y) < d.hy + margin);

    static double BoxDistance((double l, double t, double r, double b) box, double x, double y)
    {
        double cx = (box.l + box.r) / 2, cy = (box.t + box.b) / 2, hx = (box.r - box.l) / 2, hy = (box.b - box.t) / 2;
        double dx = Math.Abs(x - cx) - hx, dy = Math.Abs(y - cy) - hy;
        double outside = Math.Sqrt(Math.Max(dx, 0) * Math.Max(dx, 0) + Math.Max(dy, 0) * Math.Max(dy, 0));
        return outside + Math.Min(Math.Max(dx, dy), 0);
    }
}
