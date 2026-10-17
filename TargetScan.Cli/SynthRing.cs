using ShootCounter.TargetScan;
using SkiaSharp;

/// <summary>
/// Dev tool: synthetic test photos for the 25m PP10 and rapid-fire targets from their clean reference
/// images (slightly tilted, with drawn holes), because there are no real photos yet.
/// </summary>
static class SynthRing
{
    public static void Run(string imageDir, string outDir)
    {
        ScannerRapidFire.Log = Console.WriteLine;
        // PP10: reference 1.472 px/mm, centre (404.4, 378.6), 50 x 50 cm; holes in mm with expected value
        Test("pp10", Path.Combine(imageDir, "PP10.png"), 1.472, 404.4, 378.6, 250, outDir,
            [(5, -8, 10), (-30, 20, 9), (60, -40, 8), (60, 70, 7), (110, 40, 6), (-140, -90, 4), (200, 100, 2)],
            raster =>
            {
                var res = TargetScanner.ScanRaster(raster, raster, ScanTarget.PrecisionPP10);
                return res;
            });
        // rapid fire: reference 2.026 px/mm, centre (557.9, 531), 50 x 52 cm
        Test("rapidfire", Path.Combine(imageDir, "Schnellfeuer.png"), 2.026, 557.9, 531, 260, outDir,
            [(10, 10, 10), (-60, 30, 9), (80, -90, 8), (-100, -130, 7), (150, 120, 6), (-180, 150, 5)],
            raster => TargetScanner.ScanRaster(raster, raster, ScanTarget.RapidFire));
    }

    static void Test(string name, string referencePath, double refPxPerMm, double rcx, double rcy, double half, string outDir,
        (float x, float y, int expected)[] holes, Func<Raster, TargetScanResult?> scan)
    {
        using var reference = SKBitmap.Decode(referencePath);
        var refRaster = Raster.FromBitmap(reference);
        const int W = 1200, H = 1600;
        float s = 2.2f;
        float hf = (float)half;
        SKPoint[] tpl = [new(-hf, -hf), new(hf, -hf), new(hf, hf), new(-hf, hf)];
        SKPoint[] img =
        [
            new(600 - (float)half * s * 0.96f, 780 - (float)half * s), new(600 + (float)half * s * 0.96f, 780 - (float)half * s),
            new(600 + (float)half * s * 1.04f, 780 + (float)half * s * 0.98f), new(600 - (float)half * s * 1.04f, 780 + (float)half * s * 0.98f),
        ];
        var tplToImg = Homography.FromPoints(tpl, img);
        var imgToTpl = tplToImg.Inverse();

        var photo = new SKBitmap(W, H);
        var px = new SKColor[W * H];
        Parallel.For(0, H, y =>
        {
            for (int x = 0; x < W; x++)
            {
                var t = imgToTpl.Map(x, y);
                if (Math.Abs(t.X) > half || Math.Abs(t.Y) > half) { px[y * W + x] = new SKColor(70, 60, 50); continue; }
                var (r, g, b) = refRaster.Sample(rcx + t.X * refPxPerMm, rcy + t.Y * refPxPerMm);
                px[y * W + x] = new SKColor((byte)r, (byte)g, (byte)b);
            }
        });
        photo.Pixels = px;

        double hr = 4.5 * s;
        using (var c = new SKCanvas(photo))
        using (var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 1.0f))
        {
            using var gold = new SKPaint { Color = new SKColor(250, 220, 130), IsAntialias = true, MaskFilter = blur };
            using var rim = new SKPaint { Color = new SKColor(150, 135, 110), IsAntialias = true, MaskFilter = blur };
            using var through = new SKPaint { Color = new SKColor(225, 190, 120), IsAntialias = true, MaskFilter = blur };
            foreach (var hole in holes)
            {
                var p = tplToImg.Map(hole.x, hole.y);
                var (r, g, b) = refRaster.Sample(rcx + hole.x * refPxPerMm, rcy + hole.y * refPxPerMm);
                if ((r + g + b) / 3 < 100) c.DrawCircle(p, (float)(hr * 0.85), gold);
                else { c.DrawCircle(p, (float)hr, rim); c.DrawCircle(p, (float)(hr * 0.7), through); }
            }
        }
        var path = Path.Combine(outDir, $"synth_{name}.jpg");
        using (var data = photo.Encode(SKEncodedImageFormat.Jpeg, 92))
            File.WriteAllBytes(path, data.ToArray());

        var res = scan(Raster.Load(path, 1600));
        Console.WriteLine($"== {name}");
        if (res == null) { Console.WriteLine("no target"); return; }
        Console.WriteLine($"centre={res.TemplateToImage.Map(0, 0)} (true {tplToImg.Map(0, 0)})");
        foreach (var hole in holes)
        {
            var p = tplToImg.Map(hole.x, hole.y);
            var found = res.Hits.FirstOrDefault(x => SKPoint.Distance(x.Image, p) < 10);
            Console.WriteLine($"hole ({hole.x},{hole.y}) expected {hole.expected} -> {(found == null ? $"MISSED (manual {res.ScoreAt(p)})" : found.Score.ToString())}");
        }
        foreach (var x in res.Hits.Where(x => holes.All(o => SKPoint.Distance(x.Image, tplToImg.Map(o.x, o.y)) >= 10)))
            Console.WriteLine($"FALSE hit at ({x.Image.X:0},{x.Image.Y:0})");
        using var vis = Raster.Load(path, 1600).ToBitmap();
        using (var c = new SKCanvas(vis))
        {
            using var ring = new SKPaint { Color = SKColors.Magenta, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
            var radii = name == "pp10" ? new[] { 50.0, 100, 150, 200, 250, 300, 350, 400, 450, 500 } : new[] { 25.0, 50, 90, 130, 170, 210, 250 };
            foreach (var r in radii)
            {
                var pts = Enumerable.Range(0, 360).Select(k => res.TemplateToImage.Map(r * Math.Cos(k * Math.PI / 180), r * Math.Sin(k * Math.PI / 180))).ToArray();
                using var pth = new SKPath(); pth.AddPoly(pts, true); c.DrawPath(pth, ring);
            }
        }
        using (var data = vis.Encode(SKEncodedImageFormat.Jpeg, 85))
            File.WriteAllBytes(Path.Combine(outDir, $"synth_{name}_result.jpg"), data.ToArray());
    }
}
