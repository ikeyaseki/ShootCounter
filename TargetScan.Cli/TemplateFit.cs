using SkiaSharp;
using ShootCounter.TargetScan;

/// <summary>Measures ring outlines from a clean, undistorted image of the target (dev tool).</summary>
static class TemplateFit
{
    public static void Run(string path)
    {
        using var bmp = SKBitmap.Decode(path);
        var r = Raster.FromBitmap(bmp);
        int w = r.Width, h = r.Height;
        double s = Target25m.BlackWidth / w; // to template units
        double cx = w / 2.0, cy = h / 2.0;
        var pts = new List<(double x, double y)>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (r.Gray(y * w + x) > 110) pts.Add(((x + 0.5 - cx) * s, (y + 0.5 - cy) * s));
        Console.WriteLine($"{w}x{h} aspect={(double)h / w:0.000} points={pts.Count}");

        // centre from symmetry: refine using all points
        foreach (var ring in Target25m.Rings)
        {
            var near = pts.Where(p => Math.Abs(Target25m.StadiumDist(p.x, p.y, ring.a, ring.b)) < 15
                                      && Math.Abs(p.x) > 30 || Math.Abs(Target25m.StadiumDist(p.x, p.y, ring.a, ring.b)) < 15 && Math.Abs(p.y) > 60).ToList();
            near = near.Where(p => Math.Abs(Target25m.StadiumDist(p.x, p.y, ring.a, ring.b)) < 15).ToList();
            var (sa, sb, se) = Best(near, ring.a, ring.b, (x, y, a, b) => Target25m.StadiumDist(x, y, a, b));
            var (ea, eb, ee) = Best(near, ring.a, ring.b, Ellipse);
            Console.WriteLine($"ring {ring.value}: n={near.Count} stadium a={sa:0.0} b={sb:0.0} rms={se:0.00} | ellipse a={ea:0.0} b={eb:0.0} rms={ee:0.00}");
        }
    }

    static double Ellipse(double x, double y, double a, double b)
    {
        // approx. distance: scale radial error by local radius
        double t = Math.Sqrt(x * x / (a * a) + y * y / (b * b));
        double rho = Math.Sqrt(x * x + y * y);
        return rho * (1 - 1 / t);
    }

    static (double a, double b, double rms) Best(List<(double x, double y)> pts, double a0, double b0, Func<double, double, double, double, double> dist)
    {
        double best = double.MaxValue, ba = 0, bb = 0;
        for (double a = a0 * 0.85; a <= a0 * 1.15; a += 0.5)
            for (double b = b0 * 0.85; b <= b0 * 1.15; b += 0.5)
            {
                double e = 0;
                foreach (var p in pts) { double d = dist(p.x, p.y, a, b); e += Math.Min(d * d, 25); }
                if (e < best) { best = e; ba = a; bb = b; }
            }
        return (ba, bb, Math.Sqrt(best / pts.Count));
    }
}

static class AxisProfile
{
    /// <summary>Prints sub-pixel positions of bright lines along a row/column (intensity-weighted centroids).</summary>
    public static void Run(string path)
    {
        using var bmp = SKBitmap.Decode(path);
        var r = Raster.FromBitmap(bmp);
        int w = r.Width, h = r.Height;
        foreach (var (label, len, get) in new (string, int, Func<int, int, double>)[] {
            ("row y=c", w, (i, o) => r.Gray((h / 2 + o) * w + i)),
            ("col x=c", h, (i, o) => r.Gray(i * w + w / 2 + o)),
            ("col x=c+60", h, (i, o) => r.Gray(i * w + w / 2 + 60 + o)) })
        {
            var p = Enumerable.Range(0, len).Select(i => Enumerable.Range(-1, 3).Average(o => get(i, o))).ToArray();
            var res = new List<string>();
            for (int i = 1; i < len - 1; i++)
            {
                if (p[i] < 70 || p[i] < p[i - 1] || p[i] < p[i + 1]) continue;
                double s = 0, sw = 0;
                for (int k = Math.Max(0, i - 2); k <= Math.Min(len - 1, i + 2); k++) { double v = Math.Max(0, p[k] - 40); s += v * k; sw += v; }
                res.Add($"{s / sw:0.0}");
            }
            Console.WriteLine($"{label} (len {len}): {string.Join(" ", res)}");
        }
    }
}
