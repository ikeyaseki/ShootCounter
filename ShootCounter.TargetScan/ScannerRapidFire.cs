using SkiaSharp;

namespace ShootCounter.TargetScan;

public sealed record ScanResultRapidFire(Homography TemplateToImage, List<Hit> Hits)
{
    public double PxPerMm => SKPoint.Distance(TemplateToImage.Map(0, 0), TemplateToImage.Map(100, 0)) / 100;
    public double HoleRadiusPx => PxPerMm * TargetRapidFire.CaliberMm / 2;
}

/// <summary>
/// Finds the 25m rapid-fire target in a photo and detects the bullet holes.
/// The black disc gives the start position, its outline and the white ring lines refine the fit.
/// All scoring zones are black, so holes are bright spots (daylight through the paper), like on the
/// ordnance target. Not verified on real photos yet, only on a synthetic one.
/// </summary>
public static class ScannerRapidFire
{
    /// <summary>Optional log of the fit (development).</summary>
    public static Action<string>? Log;

    public static ScanResultRapidFire? Scan(Raster img)
    {
        int w = img.Width, h = img.Height;

        // the black disc: the largest dark, round region, with the white lines, bars and holes closed
        var dark = new bool[w * h];
        for (int i = 0; i < dark.Length; i++) dark[i] = img.Gray(i) < 80;
        int k = Math.Max(2, w / 150);
        var closed = Mask.Erode(Mask.Dilate(dark, w, h, k), w, h, k);
        var (labels, comps) = Mask.Components(closed, w, h);
        var disc = comps
            .Where(c => c.BoxW > w / 6 && c.BoxH > w / 6 && c.MinX > 0 && c.MinY > 0 && c.MaxX < w - 1 && c.MaxY < h - 1)
            .Where(c => Math.Abs(c.BoxW - c.BoxH) < 0.3 * Math.Max(c.BoxW, c.BoxH))
            .Where(c => c.Area > 0.6 * Math.PI * c.BoxW * c.BoxH / 4)
            .MaxBy(c => c.Area);
        if (disc == null) return null;

        // lines to fit: the outline of the disc and the white ring lines on it
        var ridges = TemplateFitting.FindRidges(img, brightLines: true);
        var lines = new bool[w * h];
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = y * w + x;
                if (labels[i] != disc.Label) continue;
                bool edge = labels[i - 1] != disc.Label || labels[i + 1] != disc.Label || labels[i - w] != disc.Label || labels[i + w] != disc.Label;
                lines[i] = edge || ridges[i];
            }
        var nearest = TemplateFitting.NearestMap(lines, w, h);

        double R = TargetRapidFire.BlackRadius, cx = (disc.MinX + disc.MaxX) / 2.0, cy = (disc.MinY + disc.MaxY) / 2.0;
        double rx = disc.BoxW / 2.0, ry = disc.BoxH / 2.0;
        var H = Homography.FromPoints(
            [new(-(float)R, 0), new((float)R, 0), new(0, -(float)R), new(0, (float)R)],
            [new((float)(cx - rx), (float)cy), new((float)(cx + rx), (float)cy), new((float)cx, (float)(cy - ry)), new((float)cx, (float)(cy + ry))]);
        Log?.Invoke($"disc box {disc.MinX},{disc.MinY}..{disc.MaxX},{disc.MaxY} start centre {H.Map(0, 0)}");

        // The outline alone fixes the ellipse of the disc but not where its centre lies under perspective,
        // so first the outline is fitted, then the rings inside it (with a thinned outline to keep its shape).
        // Ring lines are 40 mm apart, a search radius of a third of that cannot jump to the next ring.
        var outline = TargetRapidFire.OutlinePoints(4);
        var rings = TargetRapidFire.LinePoints(4);
        double ringPx = 40 * Math.Min(rx, ry) / R;
        foreach (var (rel, full) in new[] { (0.3, false), (0.2, true), (0.1, true) })
            H = TemplateFitting.Refine(H, outline, nearest, w, h, rel * ringPx, full) ?? H;
        var points = rings.Concat(outline.Where((_, i) => i % 4 == 0)).ToList();
        foreach (var rel in new[] { 0.35, 0.3, 0.25, 0.2, 0.15, 0.1 })
            H = TemplateFitting.Refine(H, points, nearest, w, h, rel * ringPx, full: true) ?? H;
        foreach (var r in new[] { 6.0, 5, 4 })
            H = TemplateFitting.Refine(H, points, nearest, w, h, r * w / 1200, full: true) ?? H;

        Log?.Invoke($"fitted centre {H.Map(0, 0)} px/mm {SKPoint.Distance(H.Map(0, 0), H.Map(100, 0)) / 100:0.00}");
        var result = new ScanResultRapidFire(H, []);
        result.Hits.AddRange(FindHits(img, result));
        return result;
    }

    /// <summary>
    /// Bright, hole-sized blobs on the black disc. On the white print a beige blob needs a high response,
    /// daylight through a hole is white or green and much brighter.
    /// </summary>
    static List<Hit> FindHits(Raster img, ScanResultRapidFire res)
    {
        int w = img.Width, h = img.Height;
        var inv = res.TemplateToImage.Inverse();
        double holeR = res.HoleRadiusPx;

        var ii = new long[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            long row = 0;
            for (int x = 0; x < w; x++) { row += img.Gray(y * w + x); ii[(y + 1) * (w + 1) + x + 1] = ii[y * (w + 1) + x + 1] + row; }
        }
        long Box(int x0, int y0, int x1, int y1) => ii[y1 * (w + 1) + x1] - ii[y0 * (w + 1) + x1] - ii[y1 * (w + 1) + x0] + ii[y0 * (w + 1) + x0];
        int rIn = Math.Max(1, (int)Math.Round(holeR * 0.6)), rMid = (int)Math.Ceiling(holeR * 1.5), rOut = (int)Math.Ceiling(holeR * 2.5);
        double nIn = (2 * rIn + 1) * (2 * rIn + 1), nRing = (2 * rOut + 1) * (2 * rOut + 1) - (2 * rMid + 1) * (2 * rMid + 1);

        var cands = new System.Collections.Concurrent.ConcurrentBag<(double resp, int x, int y)>();
        Parallel.For(rOut, h - rOut - 1, y =>
        {
            for (int x = rOut; x < w - rOut - 1; x++)
            {
                if (img.Gray(y * w + x) < 90) continue;
                double resp = Box(x - rIn, y - rIn, x + rIn + 1, y + rIn + 1) / nIn
                    - (Box(x - rOut, y - rOut, x + rOut + 1, y + rOut + 1) - Box(x - rMid, y - rMid, x + rMid + 1, y + rMid + 1)) / nRing;
                if (resp < 78) continue;
                var t = inv.Map(x, y);
                if (Math.Sqrt(t.X * t.X + t.Y * t.Y) > TargetRapidFire.BlackRadius - 6) continue;
                // the wide white bars respond as strongly as a hole, a hole on them has to be added by hand;
                // the thin printed lines and digits are beige and respond weakly
                if (TargetRapidFire.IsOnBar(t.X, t.Y, 3)) continue;
                if (TargetRapidFire.IsPrinted(t.X, t.Y, 2) && Scanner.IsBeige(img, x, y, rIn) && resp < 130) continue;
                cands.Add((resp, x, y));
            }
        });

        // one hit per hole, surrounded by the black disc
        var hits = new List<Hit>();
        var taken = new List<SKPoint>();
        foreach (var (resp, x, y) in cands.OrderByDescending(c => c.resp))
        {
            var p = new SKPoint(x, y);
            if (taken.Any(q => SKPoint.Distance(p, q) < holeR * 1.5)) continue;
            taken.Add(p);
            if (DarkSurround(img, x, y, holeR * 2.2) < 0.6) continue;
            var t = inv.Map(p);
            hits.Add(new Hit(p, t, resp, TargetRapidFire.Score(Math.Sqrt(t.X * t.X + t.Y * t.Y))));
        }
        return hits;
    }

    static double DarkSurround(Raster img, double cx, double cy, double r)
    {
        int dark = 0, n = 0;
        for (int k = 0; k < 48; k++)
        {
            double a = k * Math.PI / 24;
            int x = (int)(cx + r * Math.Cos(a)), y = (int)(cy + r * Math.Sin(a));
            if (x < 0 || y < 0 || x >= img.Width || y >= img.Height) continue;
            n++;
            if (img.Gray(y * img.Width + x) < 100) dark++;
        }
        return n == 0 ? 0 : (double)dark / n;
    }
}
