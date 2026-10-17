using ShootCounter.TargetScan;
using SkiaSharp;

static class Near50
{
    /// <summary>Dev tool: texture candidates near a point of the analysed image.</summary>
    public static void Run(string path, int px, int py)
    {
        var img = Raster.Load(path, 1600);
        Scanner50m.Log = s => { if (s.Contains($"({px - 2}") || s.Contains($"({px - 1}") || s.Contains($"({px},") || s.Contains($"({px + 1}") || s.Contains($"({px + 2}")) Console.WriteLine(s); };
        var res = Scanner50m.Scan(img, Raster.Load(path, 99999))!;
        foreach (var c in Texture50m.Find(img, res, 3000).Where(c => SKPoint.Distance(c.p, new SKPoint(px, py)) < 25))
            Console.WriteLine($"tex cand ({c.p.X},{c.p.Y}) tex={c.tex:0.0} dGray={c.dGray:0} warm={c.warm:0.0} ring={c.ringGray:0} peak={c.peak:0}");
        foreach (var b in Blobs50m.Find(img, res, 3000).Where(c => SKPoint.Distance(c.Image, new SKPoint(px, py)) < 25))
            Console.WriteLine($"blob cand ({b.Image.X},{b.Image.Y}) resp={b.Response:0} peak={b.Peak:0} R-B={b.R - b.B:0}");
    }
}
