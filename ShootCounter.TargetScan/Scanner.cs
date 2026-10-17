using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>
/// A detected hit. Uncertain hits (no daylight through the hole) should be confirmed by the user,
/// borderline hits lie so close to a ring line that the score may be off by one.
/// </summary>
public sealed record Hit(SKPoint Image, SKPoint Template, double Response, int Score, bool Uncertain = false, bool Borderline = false);

public sealed record ScanResult(Homography TemplateToImage, (int value, double a, double b)[] Rings, double FitScore, List<Hit> Hits, bool[] Ridges)
{
    /// <summary>Image pixels per millimetre near the target centre.</summary>
    public double PxPerMm => SKPoint.Distance(TemplateToImage.Map(0, 0), TemplateToImage.Map(100, 0)) / 100;

    /// <summary>Hole radius in image pixels near the target centre.</summary>
    public double HoleRadiusPx => PxPerMm * Target25m.CaliberMm / 2;

    /// <summary>A hit placed by hand at an image position, scored with the fitted rings.</summary>
    public Hit HitAt(SKPoint image)
    {
        var t = TemplateToImage.Inverse().Map(image);
        return new Hit(image, t, 0, Target25m.Score(t.X, t.Y, Rings));
    }
}

/// <summary>
/// Finds the 25m ordnance target in a photo and detects the bullet holes.
/// The ring template is aligned with the printed ring lines (global search, then ICP with a homography),
/// holes are found as bright blobs (daylight through the paper) and, less reliably, as textured spots.
/// </summary>
public static class Scanner
{
    public static ScanResult? Scan(Raster img)
    {
        int w = img.Width, h = img.Height;
        var ridges = FindRidges(img);
        var nearest = NearestMap(ridges, w, h);
        var fit = FitTemplate(img, ridges, nearest);
        if (fit == null) return null;

        // The paper bends and prints vary slightly: let rings 8-6 adapt their size, then re-fit the homography.
        var H = fit.Value.h;
        var rings = Target25m.Rings;
        for (int iter = 0; iter < 3; iter++)
        {
            rings = FitRings(H, ridges, w, h, rings);
            var tpl = Target25m.LinePoints(2.5, rings);
            foreach (var radius in new[] { 6.0, 5, 4 })
                H = Refine(H, tpl, nearest, w, h, radius * w / 1200, true) ?? H;
        }
        var hits = FindHits(img, H, rings);

        // the position is accurate to about 1.5 px; closer than that to a deciding line is a line shot
        double tolMm = Math.Max(0.8, 1.5 / (SKPoint.Distance(H.Map(0, 0), H.Map(100, 0)) / 100));
        hits = hits.Select(x => x with { Borderline = Target25m.LineDistance(x.Template.X, x.Template.Y, rings) < tolMm }).ToList();
        return new ScanResult(H, rings, fit.Value.score, hits, ridges);
    }

    /// <summary>
    /// Fits half-width and half-height of each ring (except 10 and 9, which anchor the scale)
    /// to the ridge pixels near it, in template coordinates.
    /// </summary>
    static (int value, double a, double b)[] FitRings(Homography H, bool[] ridges, int w, int h, (int value, double a, double b)[] current)
    {
        var inv = H.Inverse();
        var pts = new List<SKPoint>();
        for (int i = 0; i < ridges.Length; i++)
        {
            if (!ridges[i]) continue;
            var t = inv.Map(i % w, i / w);
            if (Math.Abs(t.X) < 30 || Math.Abs(t.Y) < 35) continue; // printed digits
            pts.Add(t);
        }
        var result = ((int value, double a, double b)[])current.Clone();
        for (int k = 2; k < result.Length; k++)
        {
            var (v, a0, b0) = Target25m.Rings[k];
            var (_, ac, bc) = current[k];
            var near = pts.Where(p => Math.Abs(Target25m.StadiumDist(p.X, p.Y, ac, bc)) < 12).ToList();
            if (near.Count < 50) continue;
            // coarse grid, then a fine grid around the coarse optimum
            double best = double.MaxValue, ba = ac, bb = bc;
            void Search(double aLo, double aHi, double bLo, double bHi, double step)
            {
                for (double a = aLo; a <= aHi; a += step)
                    for (double b = bLo; b <= bHi; b += step)
                    {
                        double e = 0;
                        foreach (var p in near) { double d = Target25m.StadiumDist(p.X, p.Y, a, b); e += Math.Min(d * d, 9); }
                        if (e < best) { best = e; ba = a; bb = b; }
                    }
            }
            Search(a0 * 0.9, a0 * 1.1, b0 * 0.9, b0 * 1.1, 2);
            Search(ba - 2, ba + 2, bb - 2, bb + 2, 0.5);
            result[k] = (v, ba, bb);
        }
        return result;
    }

    /// <summary>Thin bright lines on a dark background: brighter than both neighbours at distance d, in x or y.</summary>
    public static bool[] FindRidges(Raster r)
    {
        int w = r.Width, h = r.Height;
        int d = Math.Max(2, w / 400);
        var m = new bool[w * h];
        Parallel.For(d, h - d, y =>
        {
            for (int x = d; x < w - d; x++)
            {
                int i = y * w + x, g = r.Gray(i);
                if (g > 250 || g < 60) continue;
                int sx = Math.Max(r.Gray(i - d), r.Gray(i + d));
                int sy = Math.Max(r.Gray(i - d * w), r.Gray(i + d * w));
                m[i] = (g - sx > 22 && sx < 110) || (g - sy > 22 && sy < 110);
            }
        });
        return m;
    }

    /// <summary>For every pixel the index of the nearest ridge pixel (two-pass chamfer propagation).</summary>
    static int[] NearestMap(bool[] m, int w, int h)
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

    /// <summary>
    /// Aligns the ring template with the detected ring lines: multi-start ICP over initial scales,
    /// first with a similarity transform, then with a full homography. Best start wins by inlier coverage.
    /// </summary>
    static (Homography h, double score)? FitTemplate(Raster img, bool[] ridges, int[] nearest)
    {
        int w = img.Width, h = img.Height;
        var tpl = Target25m.LinePoints(2.5);
        var bands = Target25m.BandPoints(2.5);

        (Homography h, double score)? best = null;
        foreach (var start in CoarseSearch(ridges, w, h, 4))
        {
            var H = start;
            foreach (var (radius, full) in new[] { (20.0, false), (15, false), (12, true), (10, true), (8, true), (6, true), (5, true), (4, true) })
            {
                double r = radius * w / 1200;
                var next = Refine(H, tpl, nearest, w, h, r, full);
                if (next == null) break;
                H = next;
            }
            double tol = 3.0 * w / 1200;
            var (lineHit, lineIn) = Coverage(H, tpl, nearest, w, h, tol);
            var (bandHit, bandIn) = Coverage(H, bands, nearest, w, h, tol);
            double score = lineIn < tpl.Count / 3 ? 0 : (double)lineHit / tpl.Count - (double)bandHit / Math.Max(1, bandIn);
            if (best == null || score > best.Value.score) best = (H, score);
        }
        return best;
    }

    /// <summary>
    /// Exhaustive search over scale and centre (upright target) on a downscaled ridge map.
    /// Returns the best few candidates as starting points for the ICP refinement.
    /// </summary>
    static List<Homography> CoarseSearch(bool[] ridges, int w, int h, int f)
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
        var lines = Target25m.LinePoints(7.5);
        var bands = Target25m.BandPoints(7.5);

        var cands = new List<(double score, double s, int cx, int cy)>();
        var lockObj = new object();
        var scales = new List<double>();
        for (double rel = 0.4; rel <= 1.4; rel *= 1.03) scales.Add(rel * cw / Target25m.BlackWidth);
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
                    if (inside < lines.Count / 3) continue;
                    double pen = 0; int bin = 0;
                    foreach (var p in bands)
                    {
                        int x = (int)(cx + p.X * s), y = (int)(cy + p.Y * s);
                        if (x < 0 || y < 0 || x >= cw || y >= ch) continue;
                        bin++;
                        pen += Math.Max(0, 1 - dist[y * cw + x] / 1.5);
                    }
                    local.Add((sc / lines.Count - pen / Math.Max(1, bin), s, cx, cy));
                }
            local.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            lock (lockObj) cands.AddRange(local.Take(3));
        });
        return cands.OrderByDescending(c => c.score).Take(8)
            .Select(c => Homography.Similarity(c.s * f, 0, c.cx * f + f / 2.0, c.cy * f + f / 2.0)).ToList();
    }

    static Homography? Refine(Homography H, List<SKPoint> tpl, int[] nearest, int w, int h, double radius, bool full)
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

    /// <summary>Bullet holes: bright blobs (daylight through the paper) on the target area.</summary>
    /// <remarks>
    /// Blob filter tuned to the hole size: mean brightness of a small centre box minus the mean of a
    /// surrounding frame. A 9 mm hole responds strongly, thin ring lines and digit strokes only weakly,
    /// so holes on top of printed lines are still found. On printed areas a higher response is required.
    /// </remarks>
    static List<Hit> FindHits(Raster img, Homography tplToImg, (int value, double a, double b)[] rings)
    {
        int w = img.Width, h = img.Height;
        var imgToTpl = tplToImg.Inverse();
        var c0 = tplToImg.Map(0, 0); var c1 = tplToImg.Map(100, 0);
        double pxPerMm = SKPoint.Distance(c0, c1) / 100;
        double holeR = Target25m.CaliberMm / 2 * pxPerMm;

        var ii = new long[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            long row = 0;
            for (int x = 0; x < w; x++) { row += img.Gray(y * w + x); ii[(y + 1) * (w + 1) + x + 1] = ii[y * (w + 1) + x + 1] + row; }
        }
        long BoxSum(int x0, int y0, int x1, int y1) => ii[y1 * (w + 1) + x1] - ii[y0 * (w + 1) + x1] - ii[y1 * (w + 1) + x0] + ii[y0 * (w + 1) + x0];

        int rIn = Math.Max(1, (int)Math.Round(holeR * 0.6)), rMid = (int)Math.Ceiling(holeR * 1.5), rOut = (int)Math.Ceiling(holeR * 2.5);
        double nIn = (2 * rIn + 1) * (2 * rIn + 1), nRing = (2 * rOut + 1) * (2 * rOut + 1) - (2 * rMid + 1) * (2 * rMid + 1);

        var cands = new System.Collections.Concurrent.ConcurrentBag<(double resp, int x, int y)>();
        Parallel.For(rOut, h - rOut - 1, y =>
        {
            for (int x = rOut; x < w - rOut - 1; x++)
            {
                if (img.Gray(y * w + x) < 90) continue;
                double inner = BoxSum(x - rIn, y - rIn, x + rIn + 1, y + rIn + 1) / nIn;
                double outer = (BoxSum(x - rOut, y - rOut, x + rOut + 1, y + rOut + 1) - BoxSum(x - rMid, y - rMid, x + rMid + 1, y + rMid + 1)) / nRing;
                double resp = inner - outer;
                if (resp < 78) continue;
                var t = imgToTpl.Map(x, y);
                // on the black area (not at its edge), inside ring 6 plus a hole radius
                if (Math.Abs(t.X) > Target25m.BlackWidth / 2 - 8 || Math.Abs(t.Y) > Target25m.BlackHeight / 2 - 8) continue;
                if (Target25m.StadiumDist(t.X, t.Y, rings[^1].a, rings[^1].b) > Target25m.CaliberMm) continue;
                // the print is beige, daylight through a hole is white (sky) or green (grass)
                if (Target25m.IsPrinted(t.X, t.Y, rings) && IsBeige(img, x, y, rIn) && resp < 130) continue;
                cands.Add((resp, x, y));
            }
        });

        // non-maximum suppression: one hit per hole
        var hits = new List<Hit>();
        var taken = new List<SKPoint>();
        foreach (var (resp, x, y) in cands.OrderByDescending(c => c.resp))
        {
            var p = new SKPoint(x, y);
            if (taken.Any(q => SKPoint.Distance(p, q) < holeR * 1.5)) continue;
            taken.Add(p);
            if (DarkSurround(img, x, y, holeR * 2.2) < 0.6) continue; // not on the black target surface
            var tp = imgToTpl.Map(x, y);
            hits.Add(new Hit(p, tp, resp, Target25m.Score(tp.X, tp.Y, rings)));
        }
        // holes without daylight behind them, reported as uncertain
        foreach (var (resp, x, y) in FindUnlitHoles(img, imgToTpl, rings, holeR))
        {
            if (resp < 33) break;
            if (IsBeige(img, x, y, (int)holeR)) continue; // printed digit stroke
            var p = new SKPoint(x, y);
            if (taken.Any(q => SKPoint.Distance(p, q) < holeR * 2.5)) continue;
            taken.Add(p);
            var tp = imgToTpl.Map(x, y);
            hits.Add(new Hit(p, tp, resp, Target25m.Score(tp.X, tp.Y, rings), Uncertain: true));
        }
        return hits;
    }

    /// <summary>
    /// Holes without daylight behind them (e.g. the wooden board at the target edge) are not bright,
    /// but their frayed paper edge is textured while the black paper around is smooth:
    /// local standard deviation in a hole-sized box minus that of a surrounding frame.
    /// </summary>
    static List<(double resp, int x, int y)> FindUnlitHoles(Raster img, Homography imgToTpl, (int value, double a, double b)[] rings, double holeR)
    {
        int w = img.Width, h = img.Height;
        var s1 = new double[(w + 1) * (h + 1)];
        var s2 = new double[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            double r1 = 0, r2 = 0;
            for (int x = 0; x < w; x++)
            {
                double g = img.Gray(y * w + x);
                r1 += g; r2 += g * g;
                int k = (y + 1) * (w + 1) + x + 1;
                s1[k] = s1[k - w - 1] + r1; s2[k] = s2[k - w - 1] + r2;
            }
        }
        double Box(double[] s, int x0, int y0, int x1, int y1) => s[y1 * (w + 1) + x1] - s[y0 * (w + 1) + x1] - s[y1 * (w + 1) + x0] + s[y0 * (w + 1) + x0];
        int rIn = Math.Max(2, (int)Math.Round(holeR)), rMid = (int)Math.Ceiling(holeR * 1.4), rOut = (int)Math.Ceiling(holeR * 2.0);
        double nIn = (2 * rIn + 1) * (2 * rIn + 1), nRing = (2 * rOut + 1) * (2 * rOut + 1) - (2 * rMid + 1) * (2 * rMid + 1);

        var cands = new System.Collections.Concurrent.ConcurrentBag<(double resp, int x, int y)>();
        Parallel.For(rOut, h - rOut - 1, y =>
        {
            for (int x = rOut; x < w - rOut - 1; x++)
            {
                double m1 = Box(s1, x - rIn, y - rIn, x + rIn + 1, y + rIn + 1) / nIn;
                double v1 = Box(s2, x - rIn, y - rIn, x + rIn + 1, y + rIn + 1) / nIn - m1 * m1;
                if (v1 < 100) continue;
                double m2 = (Box(s1, x - rOut, y - rOut, x + rOut + 1, y + rOut + 1) - Box(s1, x - rMid, y - rMid, x + rMid + 1, y + rMid + 1)) / nRing;
                double v2 = (Box(s2, x - rOut, y - rOut, x + rOut + 1, y + rOut + 1) - Box(s2, x - rMid, y - rMid, x + rMid + 1, y + rMid + 1)) / nRing - m2 * m2;
                if (m2 > 90) continue; // surroundings must be the black paper
                double resp = Math.Sqrt(Math.Max(0, v1)) - Math.Sqrt(Math.Max(0, v2));
                if (resp < 8) continue;
                var t = imgToTpl.Map(x, y);
                if (Math.Abs(t.X) > Target25m.BlackWidth / 2 - 6 || Math.Abs(t.Y) > Target25m.BlackHeight / 2 - 6) continue;
                if (Target25m.StadiumDist(t.X, t.Y, rings[^1].a, rings[^1].b) > Target25m.CaliberMm) continue;
                if (Target25m.IsPrinted(t.X, t.Y, rings)) continue;
                cands.Add((resp, x, y));
            }
        });
        var res = new List<(double, int, int)>();
        foreach (var c in cands.OrderByDescending(c => c.resp))
        {
            if (res.Any(q => Math.Abs(q.Item2 - c.x) < holeR * 2 && Math.Abs(q.Item3 - c.y) < holeR * 2)) continue;
            res.Add(c);
        }
        return res;
    }

    /// <summary>True if the bright pixels around (cx, cy) have the warm beige tone of the printed lines and digits.</summary>
    internal static bool IsBeige(Raster img, int cx, int cy, int r)
    {
        long sr = 0, sg = 0, sb = 0; int n = 0;
        for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                int i = y * img.Width + x;
                if (img.Gray(i) < 90) continue;
                sr += img.R[i]; sg += img.G[i]; sb += img.B[i]; n++;
            }
        if (n == 0) return false;
        double R = (double)sr / n, G = (double)sg / n, B = (double)sb / n;
        return R - B > 30 && G < 0.87 * R;
    }

    /// <summary>Fraction of dark pixels on a circle around (cx, cy).</summary>
    static double DarkSurround(Raster img, double cx, double cy, double r)
    {
        int dark = 0, n = 0;
        for (int k = 0; k < 48; k++)
        {
            double t = k * Math.PI / 24;
            int x = (int)(cx + r * Math.Cos(t)), y = (int)(cy + r * Math.Sin(t));
            if (x < 0 || y < 0 || x >= img.Width || y >= img.Height) continue;
            n++;
            if (img.Gray(y * img.Width + x) < 100) dark++;
        }
        return n == 0 ? 0 : (double)dark / n;
    }
}
