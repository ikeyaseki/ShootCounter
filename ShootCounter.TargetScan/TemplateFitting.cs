using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>Points of a target template (in mm) used to align it with the printed lines in a photo.</summary>
/// <param name="Lines">Points on the printed ring lines.</param>
/// <param name="Bands">Points halfway between ring lines, where a correct fit finds no lines.</param>
/// <param name="Size">Reference size of the template in mm (used for the scale search range).</param>
internal sealed record FitTemplate(List<SKPoint> Lines, List<SKPoint> Bands, List<SKPoint> CoarseLines, List<SKPoint> CoarseBands, double Size);

/// <summary>
/// Aligns a ring template with the ring lines found in a photo: exhaustive search over scale and centre
/// on a downscaled line map, then ICP from the best starts, first with a similarity, then with a homography.
/// </summary>
internal static class TemplateFitting
{
    /// <summary>
    /// Thin lines that differ from both neighbours at distance d, in x or y:
    /// bright lines on a dark background or dark lines on a bright background.
    /// </summary>
    public static bool[] FindRidges(Raster r, bool brightLines)
    {
        int w = r.Width, h = r.Height;
        int d = Math.Max(2, w / 400);
        var m = new bool[w * h];
        Parallel.For(d, h - d, y =>
        {
            for (int x = d; x < w - d; x++)
            {
                int i = y * w + x, g = r.Gray(i);
                if (brightLines)
                {
                    if (g > 250 || g < 60) continue;
                    int sx = Math.Max(r.Gray(i - d), r.Gray(i + d));
                    int sy = Math.Max(r.Gray(i - d * w), r.Gray(i + d * w));
                    m[i] = (g - sx > 22 && sx < 110) || (g - sy > 22 && sy < 110);
                }
                else
                {
                    if (g > 170) continue;
                    int sx = Math.Min(r.Gray(i - d), r.Gray(i + d));
                    int sy = Math.Min(r.Gray(i - d * w), r.Gray(i + d * w));
                    m[i] = (sx - g > 25 && sx > 120) || (sy - g > 25 && sy > 120);
                }
            }
        });
        return m;
    }

    /// <summary>For every pixel the index of the nearest ridge pixel (two-pass chamfer propagation).</summary>
    public static int[] NearestMap(bool[] m, int w, int h)
    {
        var near = new int[w * h];
        var dist = new float[w * h];
        for (int i = 0; i < near.Length; i++) { near[i] = m[i] ? i : -1; dist[i] = m[i] ? 0 : float.MaxValue; }

        void Relax(int i, int j)
        {
            int n = near[j];
            if (n < 0) return;
            float dx = i % w - n % w, dy = i / w - n / w, d = dx * dx + dy * dy;
            if (d < dist[i]) { dist[i] = d; near[i] = n; }
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (x > 0) Relax(i, i - 1);
                if (y > 0) { Relax(i, i - w); if (x > 0) Relax(i, i - w - 1); if (x < w - 1) Relax(i, i - w + 1); }
            }
        for (int y = h - 1; y >= 0; y--)
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x;
                if (x < w - 1) Relax(i, i + 1);
                if (y < h - 1) { Relax(i, i + w); if (x < w - 1) Relax(i, i + w + 1); if (x > 0) Relax(i, i + w - 1); }
            }
        return near;
    }

    /// <param name="relMin">Smallest template size relative to the image width.</param>
    /// <param name="relMax">Largest template size relative to the image width.</param>
    public static (Homography h, double score)? Fit(bool[] ridges, int[] nearest, int w, int h, FitTemplate tpl, double relMin, double relMax)
    {
        (Homography h, double score)? best = null;
        foreach (var start in CoarseSearch(ridges, w, h, 4, tpl, relMin, relMax))
        {
            var H = start;
            foreach (var (radius, full) in new[] { (20.0, false), (15, false), (12, true), (10, true), (8, true), (6, true), (5, true), (4, true) })
            {
                double r = radius * w / 1200;
                var next = Refine(H, tpl.Lines, nearest, w, h, r, full);
                if (next == null) break;
                H = next;
            }
            double tol = 3.0 * w / 1200;
            var (lineHit, lineIn) = Coverage(H, tpl.Lines, nearest, w, h, tol);
            var (bandHit, bandIn) = Coverage(H, tpl.Bands, nearest, w, h, tol);
            double score = lineIn < tpl.Lines.Count / 4 ? 0 : (double)lineHit / Math.Max(1, lineIn) - (double)bandHit / Math.Max(1, bandIn);
            if (best == null || score > best.Value.score) best = (H, score);
        }
        return best;
    }

    /// <summary>
    /// Exhaustive search over scale and centre (no rotation) on a downscaled ridge map.
    /// Returns the best few candidates as starting points for the ICP refinement.
    /// </summary>
    static List<Homography> CoarseSearch(bool[] ridges, int w, int h, int f, FitTemplate tpl, double relMin, double relMax)
    {
        int cw = w / f, ch = h / f;
        var small = new bool[cw * ch];
        for (int y = 0; y < ch * f; y++)
            for (int x = 0; x < cw * f; x++)
                if (ridges[y * w + x]) small[y / f * cw + x / f] = true;
        var near = NearestMap(small, cw, ch);
        var dist = new float[cw * ch];
        for (int i = 0; i < dist.Length; i++)
        {
            int q = near[i];
            dist[i] = q < 0 ? 99 : MathF.Sqrt((i % cw - q % cw) * (i % cw - q % cw) + (i / cw - q / cw) * (i / cw - q / cw));
        }
        var lines = tpl.CoarseLines;
        var bands = tpl.CoarseBands;

        var cands = new List<(double score, double s, int cx, int cy)>();
        var lockObj = new object();
        var scales = new List<double>();
        for (double rel = relMin; rel <= relMax; rel *= 1.03) scales.Add(rel * cw / tpl.Size);
        Parallel.ForEach(scales, s =>
        {
            var local = new List<(double, double, int, int)>();
            for (int cy = ch / 5; cy < ch * 4 / 5; cy += 2)
                for (int cx = cw / 5; cx < cw * 4 / 5; cx += 2)
                {
                    double sc = 0; int inside = 0;
                    foreach (var p in lines)
                    {
                        int x = (int)(cx + p.X * s), y = (int)(cy + p.Y * s);
                        if (x < 0 || y < 0 || x >= cw || y >= ch) continue;
                        inside++;
                        sc += Math.Max(0, 1 - dist[y * cw + x] / 1.5);
                    }
                    if (inside < lines.Count / 4) continue;
                    double pen = 0; int bin = 0;
                    foreach (var p in bands)
                    {
                        int x = (int)(cx + p.X * s), y = (int)(cy + p.Y * s);
                        if (x < 0 || y < 0 || x >= cw || y >= ch) continue;
                        bin++;
                        pen += Math.Max(0, 1 - dist[y * cw + x] / 1.5);
                    }
                    local.Add((sc / inside - pen / Math.Max(1, bin), s, cx, cy));
                }
            local.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            lock (lockObj) cands.AddRange(local.Take(3));
        });
        return cands.OrderByDescending(c => c.score).Take(8)
            .Select(c => Homography.Similarity(c.s * f, 0, c.cx * f + f / 2.0, c.cy * f + f / 2.0)).ToList();
    }

    public static Homography? Refine(Homography H, List<SKPoint> tpl, int[] nearest, int w, int h, double radius, bool full)
    {
        var src = new List<SKPoint>(); var dst = new List<SKPoint>(); var wt = new List<double>();
        foreach (var t in tpl)
        {
            var p = H.Map(t);
            int px = (int)Math.Round(p.X), py = (int)Math.Round(p.Y);
            if (px < 0 || py < 0 || px >= w || py >= h) continue;
            int q = nearest[py * w + px];
            if (q < 0) continue;
            var qp = new SKPoint(q % w, q / w);
            double d = SKPoint.Distance(p, qp);
            if (d > radius) continue;
            double u = d / radius;
            src.Add(t); dst.Add(qp); wt.Add((1 - u * u) * (1 - u * u));
        }
        if (src.Count < 30) return null;
        return full ? Homography.FromPoints(src, dst, wt) : FitSimilarity(src, dst, wt);
    }

    /// <summary>Weighted least-squares similarity (Umeyama, 2D).</summary>
    static Homography FitSimilarity(List<SKPoint> src, List<SKPoint> dst, List<double> wt)
    {
        double W = wt.Sum();
        double msx = 0, msy = 0, mdx = 0, mdy = 0;
        for (int i = 0; i < src.Count; i++) { msx += wt[i] * src[i].X; msy += wt[i] * src[i].Y; mdx += wt[i] * dst[i].X; mdy += wt[i] * dst[i].Y; }
        msx /= W; msy /= W; mdx /= W; mdy /= W;
        double a = 0, b = 0, ss = 0;
        for (int i = 0; i < src.Count; i++)
        {
            double x = src[i].X - msx, y = src[i].Y - msy, u = dst[i].X - mdx, v = dst[i].Y - mdy;
            a += wt[i] * (x * u + y * v); b += wt[i] * (x * v - y * u); ss += wt[i] * (x * x + y * y);
        }
        double angle = Math.Atan2(b, a), s = Math.Sqrt(a * a + b * b) / ss;
        double c = s * Math.Cos(angle), si = s * Math.Sin(angle);
        return Homography.Similarity(s, angle, mdx - (c * msx - si * msy), mdy - (si * msx + c * msy));
    }

    /// <summary>Number of template points with a ridge pixel within tol, and number of points inside the image.</summary>
    static (int hit, int inside) Coverage(Homography H, List<SKPoint> tpl, int[] nearest, int w, int h, double tol)
    {
        int inside = 0, hit = 0;
        foreach (var t in tpl)
        {
            var p = H.Map(t);
            int px = (int)Math.Round(p.X), py = (int)Math.Round(p.Y);
            if (px < 0 || py < 0 || px >= w || py >= h) continue;
            inside++;
            int q = nearest[py * w + px];
            if (q >= 0 && SKPoint.Distance(p, new SKPoint(q % w, q / w)) <= tol) hit++;
        }
        return (hit, inside);
    }
}
