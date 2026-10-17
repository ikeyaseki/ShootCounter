using ShootCounter.TargetScan;
using SkiaSharp;

/// <summary>
/// Dev tool: synthetic photo of a cut-out Morgarten figure from the clean reference image (in front of a
/// dark backstop, slightly tilted, with drawn holes), because there are no real photos yet.
/// </summary>
static class SynthM
{
    public static void Run(string referencePath, string outDir)
    {
        using var reference = SKBitmap.Decode(referencePath);
        var refRaster = Raster.FromBitmap(reference);
        // reference: 1.25 mm per pixel, ring centre at (190, 471.5)
        double refPxPerMm = 1 / 1.25, rcx = 190, rcy = 471.5;
        const int W = 1200, H = 1600;
        double s = 1.35;
        SKPoint[] tpl = [new(-250, -600), new(250, -600), new(250, 430), new(-250, 430)];
        SKPoint[] img =
        [
            new(600 - 250 * (float)s * 0.96f, 820 - 600 * (float)s), new(600 + 250 * (float)s * 0.96f, 820 - 600 * (float)s),
            new(600 + 250 * (float)s * 1.04f, 820 + 430 * (float)s * 0.98f), new(600 - 250 * (float)s * 1.04f, 820 + 430 * (float)s * 0.98f),
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
                // cut out: outside the figure the dark backstop shows
                if (TargetMorgarten.FigureDistance(t.X, t.Y) > 0) { px[y * W + x] = new SKColor(70, 60, 50); continue; }
                var (r, g, b) = refRaster.Sample(rcx + t.X * refPxPerMm, rcy + t.Y * refPxPerMm);
                px[y * W + x] = new SKColor((byte)r, (byte)g, (byte)b);
            }
        });
        photo.Pixels = px;

        (float x, float y, int expected)[] holes =
        [
            (10, -20, 5), (-60, 110, 4), (150, 60, 3), (-200, -250, 2), (0, -500, 1), (190, 380, 1), (230, 0, 0),
        ];
        double hr = TargetMorgarten.CaliberMm / 2 * s;
        using (var c = new SKCanvas(photo))
        {
            using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.8f);
            using var dark = new SKPaint { Color = new SKColor(60, 50, 40), IsAntialias = true, MaskFilter = blur };
            using var rim = new SKPaint { Color = new SKColor(120, 140, 100), IsAntialias = true, MaskFilter = blur };
            foreach (var hole in holes)
            {
                var p = tplToImg.Map(hole.x, hole.y);
                c.DrawCircle(p, (float)(hr * 1.05), rim);
                c.DrawCircle(p, (float)(hr * 0.85), dark);
            }
        }
        var path = Path.Combine(outDir, "synth_morgarten.jpg");
        using (var data = photo.Encode(SKEncodedImageFormat.Jpeg, 92))
            File.WriteAllBytes(path, data.ToArray());

        var raster = Raster.Load(path, 1600);
        var res = ScannerMorgarten.Scan(raster);
        if (res == null) { Console.WriteLine("no target"); return; }
        Console.WriteLine($"px/mm={res.PxPerMm:0.00} (true ~{s})  centre={res.TemplateToImage.Map(0, 0)} (true {tplToImg.Map(0, 0)})");
        foreach (var hole in holes)
        {
            var p = tplToImg.Map(hole.x, hole.y);
            var found = res.Hits.FirstOrDefault(x => SKPoint.Distance(x.Image, p) < 10);
            Console.WriteLine($"hole ({hole.x},{hole.y}) expected {hole.expected} -> {(found == null ? "MISSED" : found.Score.ToString())}");
        }
        foreach (var x in res.Hits.Where(x => holes.All(o => SKPoint.Distance(x.Image, tplToImg.Map(o.x, o.y)) >= 10)))
            Console.WriteLine($"FALSE hit at ({x.Image.X:0},{x.Image.Y:0}) tpl=({x.Template.X:0},{x.Template.Y:0})");

        using var vis = raster.ToBitmap();
        using (var c = new SKCanvas(vis))
        {
            using var ring = new SKPaint { Color = SKColors.Magenta, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
            foreach (var g in TargetMorgarten.Rings)
            {
                var pts = Enumerable.Range(0, 360).Select(k => res.TemplateToImage.Map(g.a * Math.Cos(k * Math.PI / 180), g.b * Math.Sin(k * Math.PI / 180))).ToArray();
                using var pth = new SKPath(); pth.AddPoly(pts, true); c.DrawPath(pth, ring);
            }
            var outline = TargetMorgarten.OutlinePoints(5).Select(res.TemplateToImage.Map).ToArray();
            using (var pth = new SKPath()) { pth.AddPoly(outline, true); c.DrawPath(pth, ring); }
            using var hit = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true };
            foreach (var x in res.Hits) c.DrawCircle(x.Image, 16, hit);
        }
        using (var data = vis.Encode(SKEncodedImageFormat.Jpeg, 85))
            File.WriteAllBytes(Path.Combine(outDir, "synth_morgarten_result.jpg"), data.ToArray());
    }
}
