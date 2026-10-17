using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>The targets that can be recognized in a photo.</summary>
public enum ScanTarget
{
    /// <summary>Swiss 25m ordnance rapid-fire target, rings 6-10.</summary>
    Ordnance25m,
    /// <summary>Swiss 50m precision target scored 1-10.</summary>
    PrecisionP10,
    /// <summary>The same 50m target with the printed P4 scoring 1-4.</summary>
    PrecisionP4,
    /// <summary>Swiss 50m combination target (green figure) with the B5 scoring 1-5.</summary>
    CombinationB5,
    /// <summary>The same combination target scored by the rings 1-10.</summary>
    CombinationB10,
    /// <summary>Morgarten target: cut-out green figure with ellipses, 0-5.</summary>
    Morgarten,
    /// <summary>25m rapid-fire target (ISSF): black disc with white rings, 5-10.</summary>
    RapidFire,
    /// <summary>25m precision target PP10 (ISSF): rings 1-10, black disc 7-10.</summary>
    PrecisionPP10,
}

/// <summary>Result of a scan, independent of the target type.</summary>
/// <param name="templateArea">The interesting part of the target in template coordinates (mm), for display.</param>
public sealed class TargetScanResult(Homography templateToImage, double holeRadiusPx, SKRect templateArea, List<Hit> hits, Func<SKPoint, int> scoreAt)
{
    public Homography TemplateToImage { get; } = templateToImage;
    public double HoleRadiusPx { get; } = holeRadiusPx;
    public SKRect TemplateArea { get; } = templateArea;
    public List<Hit> Hits { get; } = hits;

    /// <summary>Score of a hit placed by hand at an image position.</summary>
    public int ScoreAt(SKPoint image) => scoreAt(image);
}

public static class TargetScanner
{
    /// <summary>Longer side of the analysed photo in pixels, enough for 9mm holes on a target filling the frame.</summary>
    public const int AnalysisSize = 1600;

    /// <summary>
    /// Higher resolution used by the 50m scanner to tell sharp holes from blurred glow. Most phone photos are
    /// about 4000 px, so this is close to full size.
    /// </summary>
    const int DetailSize = 4096;

    /// <summary>Scans a photo; returns the upright photo at analysis size and the result (null if no target was found).</summary>
    public static (SKBitmap photo, TargetScanResult? result) Scan(Stream photo, ScanTarget target)
    {
        // the decoder closes the stream it reads, so every decode gets its own stream over the same bytes
        byte[] bytes;
        using (var copy = new MemoryStream())
        {
            photo.CopyTo(copy);
            bytes = copy.ToArray();
        }

        var bmp = Raster.LoadBitmap(new MemoryStream(bytes), AnalysisSize);
        var img = Raster.FromBitmap(bmp);
        // only the scanners for targets with holes on light paper need the detail image
        bool needsDetail = target is not (ScanTarget.Ordnance25m or ScanTarget.Morgarten or ScanTarget.RapidFire);
        if (!needsDetail)
            return (bmp, ScanRaster(img, img, target));

        Raster detail;
        using (var big = Raster.LoadBitmap(new MemoryStream(bytes), DetailSize))
            detail = Raster.FromBitmap(big);
        return (bmp, ScanRaster(img, detail, target));
    }

    /// <summary>Scans an already decoded photo (analysis size) with its detail image (a higher resolution of it).</summary>
    public static TargetScanResult? ScanRaster(Raster img, Raster detail, ScanTarget target)
    {
        switch (target)
        {
            case ScanTarget.Ordnance25m: return From25m(Scanner.Scan(img));
            case ScanTarget.Morgarten: return FromMorgarten(ScannerMorgarten.Scan(img));
            case ScanTarget.RapidFire: return FromRapidFire(ScannerRapidFire.Scan(img));
        }
        var face = target switch
        {
            ScanTarget.CombinationB5 or ScanTarget.CombinationB10 => Face50m.Combination,
            ScanTarget.PrecisionPP10 => Face50m.PrecisionPP10,
            _ => Face50m.Precision,
        };
        return From50m(Scanner50m.Scan(img, detail, face), target);
    }

    static TargetScanResult? FromRapidFire(ScanResultRapidFire? res)
    {
        if (res == null) return null;
        var inv = res.TemplateToImage.Inverse();
        static double Distance(SKPoint t) => Math.Sqrt(t.X * t.X + t.Y * t.Y);
        double tolMm = Math.Max(0.8, 1.5 / res.PxPerMm);
        var hits = res.Hits.Select(h => h with { Borderline = TargetRapidFire.LineDistance(Distance(h.Template)) < tolMm }).ToList();
        return new TargetScanResult(res.TemplateToImage, res.HoleRadiusPx, new SKRect(-260, -260, 260, 260), hits,
            p => TargetRapidFire.Score(Distance(inv.Map(p))));
    }

    static TargetScanResult? From25m(ScanResult? res)
    {
        if (res == null) return null;
        return new TargetScanResult(res.TemplateToImage, res.HoleRadiusPx, new SKRect(-245, -400, 245, 400), res.Hits,
            p => res.HitAt(p).Score);
    }

    static TargetScanResult? FromMorgarten(ScanResultMorgarten? res)
    {
        if (res == null) return null;
        var inv = res.TemplateToImage.Inverse();
        // the shapes are only known to a few mm, so line shots are flagged generously
        double tolMm = Math.Max(2, 1.5 / res.PxPerMm);
        var hits = res.Hits.Select(h => h with { Borderline = TargetMorgarten.LineDistance(h.Template.X, h.Template.Y) < tolMm }).ToList();
        return new TargetScanResult(res.TemplateToImage, res.HoleRadiusPx, TargetMorgarten.Bounds, hits,
            p => { var t = inv.Map(p); return TargetMorgarten.Score(t.X, t.Y); });
    }

    static TargetScanResult? From50m(ScanResult50m? res, ScanTarget target)
    {
        if (res == null) return null;
        var inv = res.TemplateToImage.Inverse();
        static double Distance(SKPoint t) => Math.Sqrt(t.X * t.X + t.Y * t.Y);
        // on the half-size PP10 face the 9 mm hole is 18 template units wide: 4.5 units more radius
        // than the P10 functions assume
        int Score(SKPoint t) => target switch
        {
            ScanTarget.PrecisionP4 => Target50m.P10ToP4(Target50m.ScoreP10(Distance(t))),
            ScanTarget.CombinationB5 => TargetB.ScoreB5(t.X, t.Y),
            ScanTarget.PrecisionPP10 => Target50m.ScoreP10(Distance(t) - Target50m.CaliberMm / 2),
            _ => Target50m.ScoreP10(Distance(t)),
        };
        double LineDistance(SKPoint t) => target switch
        {
            ScanTarget.PrecisionP4 => Target50m.LineDistanceP4(Distance(t)),
            ScanTarget.PrecisionPP10 => Target50m.LineDistance(Distance(t) - Target50m.CaliberMm / 2),
            // the figure outline is only known to a few mm, so its line shots are flagged generously
            ScanTarget.CombinationB5 => Math.Min(TargetB.LineDistanceB5(t.X, t.Y),
                Math.Abs(TargetB.FigureDistance(t.X, t.Y) - Target50m.CaliberMm / 2) - 3),
            _ => Target50m.LineDistance(Distance(t)),
        };
        // the position is accurate to about 1.5 px; closer than that to a deciding line is a line shot
        double tolMm = Math.Max(0.8, 1.5 / res.PxPerMm);
        var hits = res.Hits.Select(h => h with { Score = Score(h.Template), Borderline = LineDistance(h.Template) < tolMm }).ToList();
        // show the rings up to the 5 (radius 300 mm), on the combination target also the head of the figure
        var area = res.Face == Face50m.Combination ? new SKRect(-310, -370, 310, 310) : new SKRect(-310, -310, 310, 310);
        return new TargetScanResult(res.TemplateToImage, res.HoleRadiusPx, area, hits, p => Score(inv.Map(p)));
    }
}
