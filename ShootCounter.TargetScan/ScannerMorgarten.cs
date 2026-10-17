using SkiaSharp;

namespace ShootCounter.TargetScan;

public sealed record ScanResultMorgarten(Homography TemplateToImage, List<Hit> Hits)
{
    public double PxPerMm => SKPoint.Distance(TemplateToImage.Map(0, 0), TemplateToImage.Map(100, 0)) / 100;
    public double HoleRadiusPx => PxPerMm * TargetMorgarten.CaliberMm / 2;
}

/// <summary>
/// Finds the green Morgarten figure in a photo and detects the bullet holes.
/// The figure is found by its colour, its outline gives the start position, and the outline together with
/// the white ring ellipses refine the fit. Holes are spots on the figure that are not green (whatever is
/// behind the target shows through), excluding the printed white lines and digits.
/// Not verified on real photos yet, only on a synthetic one.
/// </summary>
public static class ScannerMorgarten
{
    public static ScanResultMorgarten? Scan(Raster img)
    {
        int w = img.Width, h = img.Height;
        var green = new bool[w * h];
        for (int i = 0; i < green.Length; i++) green[i] = IsGreen(img, i);

        // the figure: the largest green region, holes and white lines closed
        int k = Math.Max(2, w / 150);
        var closed = Mask.Erode(Mask.Dilate(green, w, h, k), w, h, k);
        var (labels, comps) = Mask.Components(closed, w, h);
        var fig = comps.Where(c => c.BoxW > w / 6 && c.BoxH > h / 6).MaxBy(c => c.Area);
        if (fig == null) return null;
        var figure = new bool[w * h];
        for (int i = 0; i < figure.Length; i++) figure[i] = labels[i] == fig.Label;

        // lines to fit: the outline of the figure and the white ring lines on it
        var ridges = TemplateFitting.FindRidges(img, brightLines: true);
        var lines = new bool[w * h];
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = y * w + x;
                if (!figure[i]) continue;
                bool edge = !figure[i - 1] || !figure[i + 1] || !figure[i - w] || !figure[i + w];
                lines[i] = edge || ridges[i];
            }
        var nearest = TemplateFitting.NearestMap(lines, w, h);

        // start: the bounding box of the figure; then ICP, first the outline only, then with the rings
        var b = TargetMorgarten.Bounds;
        var H = Homography.FromPoints(
            [new(b.Left, b.Top), new(b.Right, b.Top), new(b.Right, b.Bottom), new(b.Left, b.Bottom)],
            [new(fig.MinX, fig.MinY), new(fig.MaxX, fig.MinY), new(fig.MaxX, fig.MaxY), new(fig.MinX, fig.MaxY)]);
        var outline = TargetMorgarten.OutlinePoints(4);
        var all = outline.Concat(TargetMorgarten.RingPoints(4)).ToList();
        double figPx = Math.Max(fig.BoxW, fig.BoxH);
        foreach (var (rel, full) in new[] { (0.06, false), (0.04, false), (0.03, true), (0.02, true) })
            H = TemplateFitting.Refine(H, outline, nearest, w, h, rel * figPx, full) ?? H;
        foreach (var rel in new[] { 0.02, 0.015, 0.01, 0.007, 0.005 })
            H = TemplateFitting.Refine(H, all, nearest, w, h, rel * figPx, full: true) ?? H;

        var result = new ScanResultMorgarten(H, []);
        result.Hits.AddRange(FindHits(img, figure, ridges, result));
        return result;
    }

    /// <summary>Green print: green clearly above red and blue.</summary>
    static bool IsGreen(Raster img, int i) => img.G[i] > img.R[i] + 15 && img.G[i] > img.B[i] + 5;

    static List<Hit> FindHits(Raster img, bool[] figure, bool[] ridges, ScanResultMorgarten res)
    {
        int w = img.Width, h = img.Height;
        var inv = res.TemplateToImage.Inverse();
        double holeR = res.HoleRadiusPx, area = Math.PI * holeR * holeR;
        var lineMask = Mask.Dilate(ridges, w, h, 1);

        // not green, inside the figure, away from its edge, the printed lines and digits
        var spot = new bool[w * h];
        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (!figure[i] || IsGreen(img, i) || lineMask[i]) continue;
                var t = inv.Map(x, y);
                if (TargetMorgarten.FigureDistance(t.X, t.Y) > -3) continue;
                if (TargetMorgarten.IsNearPrint(t.X, t.Y, 2)) continue;
                if (TargetMorgarten.Rings.Any(g => Math.Abs(TargetMorgarten.EllipseDistance(t.X, t.Y, g.a, g.b)) < 2)) continue;
                spot[i] = true;
            }
        });

        var (_, comps) = Mask.Components(spot, w, h);
        var hits = new List<Hit>();
        foreach (var c in comps)
        {
            // hole sized (a hole on a ring line loses the line pixels) and roughly round
            if (c.Area < area * 0.25 || c.Area > area * 3) continue;
            double size = Math.Max(c.BoxW, c.BoxH);
            if (size > holeR * 3.5 || c.Area < 0.3 * size * size) continue;
            // surrounded by green print
            if (GreenSurround(img, c.Cx, c.Cy, holeR * 2) < 0.6) continue;
            var p = new SKPoint((float)c.Cx, (float)c.Cy);
            var t = inv.Map(p);
            hits.Add(new Hit(p, t, c.Area / area, TargetMorgarten.Score(t.X, t.Y)));
        }
        return hits;
    }

    static double GreenSurround(Raster img, double cx, double cy, double r)
    {
        int green = 0, n = 0;
        for (int k = 0; k < 48; k++)
        {
            double a = k * Math.PI / 24;
            int x = (int)(cx + r * Math.Cos(a)), y = (int)(cy + r * Math.Sin(a));
            if (x < 0 || y < 0 || x >= img.Width || y >= img.Height) continue;
            n++;
            if (IsGreen(img, y * img.Width + x)) green++;
        }
        return n == 0 ? 0 : (double)green / n;
    }
}
