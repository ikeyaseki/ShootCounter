namespace ShootCounter.TargetScan;

/// <summary>
/// Geometry of the Swiss 50m combination target B5/B10 (1 x 1 m), in millimetres, origin at the centre,
/// y pointing down. The ring circles are the same as on the P10 target (radius 50 mm per ring).
/// Printed on top: a dark green figure (body and head), in it a light green ring up to the 8 line
/// (radius 150 mm) and a dark centre disc up to the middle of the 9 (radius 75 mm).
/// The figure was measured on a low-resolution reference image (about 2 mm per pixel), so its edges
/// are only accurate to a few millimetres.
/// </summary>
public static class TargetB
{
    public const double CentreRadius = 75;
    public const double LightRingRadius = 150;

    // body and head as axis-aligned rectangles (left, top, right, bottom)
    static readonly (double l, double t, double r, double b) Body = (-225, -235, 225, 175);
    static readonly (double l, double t, double r, double b) Head = (-100, -355, 100, -235);

    /// <summary>B10 value: the P10 rings.</summary>
    public static int ScoreB10(double x, double y) => Target50m.ScoreP10(Math.Sqrt(x * x + y * y));

    /// <summary>
    /// B5 value of a hit centred at (x, y); a hole touching a boundary counts the higher value.
    /// 5: the 10 and the inner half of the 9 (dark centre disc), 4: up to the 8 line (light green ring),
    /// 3: the rest of the green figure, 2: outside the figure up to the 4 line, 1: rings 3 to 1.
    /// </summary>
    public static int ScoreB5(double x, double y)
    {
        double d = Math.Sqrt(x * x + y * y), r = Target50m.CaliberMm / 2;
        if (d - r <= CentreRadius) return 5;
        if (d - r <= LightRingRadius) return 4;
        if (FigureDistance(x, y) <= r) return 3;
        if (d - r <= Target50m.Radius(4)) return 2;
        if (d - r <= Target50m.Radius(1)) return 1;
        return 0;
    }

    /// <summary>Distance in mm from the hole edge to the nearest boundary that changes the B5 value.</summary>
    public static double LineDistanceB5(double x, double y)
    {
        double d = Math.Sqrt(x * x + y * y), r = Target50m.CaliberMm / 2;
        return new[]
        {
            Math.Abs(d - r - CentreRadius), Math.Abs(d - r - LightRingRadius), Math.Abs(FigureDistance(x, y) - r),
            Math.Abs(d - r - Target50m.Radius(4)), Math.Abs(d - r - Target50m.Radius(1)),
        }.Min();
    }

    /// <summary>Signed distance to the outline of the figure (negative inside).</summary>
    public static double FigureDistance(double x, double y) => Math.Min(BoxDistance(Body, x, y), BoxDistance(Head, x, y));

    static double BoxDistance((double l, double t, double r, double b) box, double x, double y)
    {
        double cx = (box.l + box.r) / 2, cy = (box.t + box.b) / 2, hx = (box.r - box.l) / 2, hy = (box.b - box.t) / 2;
        double dx = Math.Abs(x - cx) - hx, dy = Math.Abs(y - cy) - hy;
        double outside = Math.Sqrt(Math.Max(dx, 0) * Math.Max(dx, 0) + Math.Max(dy, 0) * Math.Max(dy, 0));
        return outside + Math.Min(Math.Max(dx, dy), 0);
    }
}
