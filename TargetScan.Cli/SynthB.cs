using ShootCounter.TargetScan;
using SkiaSharp;

/// <summary>
/// Dev tool: synthetic B5/B10 test photo from the clean reference image (slightly tilted, with drawn holes),
/// because there are no real photos of shot B targets yet.
/// </summary>
static class SynthB
{
    public static void Run(string referencePath, string outDir)
    {
        using var reference = SKBitmap.Decode(referencePath);
        // reference: 1 m across ~ 500 px, centre measured at (249.5, 250)
        double refPxPerMm = 500.0 / 1020, rcx = 249.5, rcy = 250;
        const int W = 1200, H = 1600;
        double pxPerMm = 1.9;
        // photo: target centre at (600, 760), slight perspective (taken from below)
        SKPoint[] tpl = [new(-400, -400), new(400, -400), new(400, 400), new(-400, 400)];
        SKPoint[] img = [new(600 - 400 * 1.9f * 0.97f, 760 - 400 * 1.9f), new(600 + 400 * 1.9f * 0.97f, 760 - 400 * 1.9f), new(600 + 400 * 1.9f * 1.03f, 760 + 400 * 1.9f * 0.98f), new(600 - 400 * 1.9f * 1.03f, 760 + 400 * 1.9f * 0.98f)];
        var tplToImg = Homography.FromPoints(tpl, img);
        var imgToTpl = tplToImg.Inverse();

        var photo = new SKBitmap(W, H);
        var px = new SKColor[W * H];
        var refRaster = Raster.FromBitmap(reference);
        Parallel.For(0, H, y =>
        {
            for (int x = 0; x < W; x++)
            {
                var t = imgToTpl.Map(x, y);
                var (r, g, b) = refRaster.Sample(rcx + t.X * refPxPerMm, rcy + t.Y * refPxPerMm);
                // backlit paper is warmer and brighter than the scan
                px[y * W + x] = Math.Abs(t.X) > 500 || Math.Abs(t.Y) > 500
                    ? new SKColor(60, 45, 30)
                    : new SKColor((byte)Math.Min(255, r * 1.05 + 8), (byte)Math.Min(255, g * 1.0 + 4), (byte)Math.Min(255, b * 0.92));
            }
        });
        photo.Pixels = px;

        // holes: (template x, y, expected B5, expected B10)
        (float x, float y, int b5, int b10)[] holes =
        [
            (20, -30, 5, 10),     // dark centre disc
            (-110, 40, 4, 8),     // light green ring
            (180, 120, 3, 7),     // dark figure body
            (40, -300, 3, 5),     // head
            (300, -60, 2, 5),     // white, outside the figure
            (-330, 200, 2, 3),    // white, ring 3 (below the 4 line on the B5 face counts 2: 4 line has radius 350)
        ];
        using (var c = new SKCanvas(photo))
        {
            double hr = Target50m.CaliberMm / 2 * pxPerMm;
            foreach (var hole in holes)
            {
                var p = tplToImg.Map(hole.x, hole.y);
                bool dark = Target50m.IsDarkPrint(Face50m.Combination, hole.x, hole.y);
                if (dark)
                {
                    using var gold = new SKPaint { Color = new SKColor(250, 215, 120), IsAntialias = true };
                    c.DrawCircle(p, (float)(hr * 0.8), gold);
                }
                else
                {
                    // golden background through the hole, frayed dark rim
                    using var blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 1.2f);
                    using var rim = new SKPaint { Color = new SKColor(165, 150, 130), IsAntialias = true, MaskFilter = blur };
                    using var gold = new SKPaint { Color = new SKColor(225, 190, 120), IsAntialias = true, MaskFilter = blur };
                    c.DrawCircle(p, (float)hr, rim);
                    c.DrawCircle(p, (float)(hr * 0.7), gold);
                    using var spark = new SKPaint { Color = new SKColor(250, 235, 190), IsAntialias = true };
                    c.DrawCircle(new SKPoint(p.X - 2, p.Y - 2), (float)(hr * 0.3), spark);
                }
            }
        }

        var path = Path.Combine(outDir, "synth_b.jpg");
        using (var data = photo.Encode(SKEncodedImageFormat.Jpeg, 92))
            File.WriteAllBytes(path, data.ToArray());

        var raster = Raster.Load(path, 1600);
        Scanner50m.Log = s => Console.WriteLine("   " + s);
        var res = Scanner50m.Scan(raster, raster, Face50m.Combination);
        if (res == null) { Console.WriteLine("no target"); return; }
        var texAll = Texture50m.Find(raster, res, 5000);
        foreach (var o in holes) { var q = tplToImg.Map(o.x, o.y); foreach (var c in texAll.Where(c => SKPoint.Distance(c.p, q) < 12)) Console.WriteLine($"   tex near ({o.x},{o.y}): tex={c.tex:0.0} dGray={c.dGray:0} ring={c.ringGray:0}"); }
        var fit = res.TemplateToImage.Inverse();
        Console.WriteLine($"px/mm={res.PxPerMm:0.00} (true {pxPerMm})  centre={res.TemplateToImage.Map(0, 0)} (true {tplToImg.Map(0, 0)})");
        foreach (var hole in holes)
        {
            var p = tplToImg.Map(hole.x, hole.y);
            var found = res.Hits.FirstOrDefault(h => SKPoint.Distance(h.Image, p) < 10);
            string got = found == null ? "MISSED" : $"B5={TargetB.ScoreB5(found.Template.X, found.Template.Y)} B10={TargetB.ScoreB10(found.Template.X, found.Template.Y)}";
            Console.WriteLine($"hole ({hole.x},{hole.y}) expected B5={hole.b5} B10={hole.b10} -> {got}");
        }
        foreach (var h in res.Hits.Where(h => holes.All(o => SKPoint.Distance(h.Image, tplToImg.Map(o.x, o.y)) >= 10)))
            Console.WriteLine($"FALSE hit at ({h.Image.X:0},{h.Image.Y:0}) tpl=({h.Template.X:0},{h.Template.Y:0})");

        using var vis = raster.ToBitmap();
        using (var c = new SKCanvas(vis))
        {
            using var ring = new SKPaint { Color = SKColors.Lime, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
            foreach (var r in new[] { 75.0, 150, 200, 250, 300, 350, 400, 450, 500 })
            {
                var pts = Enumerable.Range(0, 360).Select(k => res.TemplateToImage.Map(r * Math.Cos(k * Math.PI / 180), r * Math.Sin(k * Math.PI / 180))).ToArray();
                using var pth = new SKPath(); pth.AddPoly(pts, true); c.DrawPath(pth, ring);
            }
            using var hitPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true };
            foreach (var h in res.Hits) c.DrawCircle(h.Image, 16, hitPaint);
        }
        using (var data = vis.Encode(SKEncodedImageFormat.Jpeg, 85))
            File.WriteAllBytes(Path.Combine(outDir, "synth_b_result.jpg"), data.ToArray());
    }
}
