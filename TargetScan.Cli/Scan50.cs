using SkiaSharp;
using ShootCounter.TargetScan;

/// <summary>Dev tool: runs the 50m scanner on the round-target photos and renders the result.</summary>
static class Scan50
{
    public static void Run(string dir, string outDir, string? filter)
    {
        var files = Directory.GetFiles(dir, "*.jpg").OrderBy(f => f).Take(10).Where(f => filter == null || f.Contains(filter)).ToArray();
        var tiles = new List<SKBitmap>();
        foreach (var f in files)
        {
            var name = Path.GetFileNameWithoutExtension(f)[13..19];
            var img = Raster.Load(f, 1600);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var res = Scanner50m.Scan(img, Raster.Load(f, 99999));
            sw.Stop();
            if (res == null) { Console.WriteLine($"{name}: no target"); continue; }
            Console.WriteLine($"{name}: {sw.ElapsedMilliseconds}ms hits={res.Hits.Count} [{string.Join(",", res.Hits.Select(x => x.Score))}]");

            var bmp = img.ToBitmap();
            using (var c = new SKCanvas(bmp))
            {
                using var ring = new SKPaint { Color = SKColors.Lime.WithAlpha(170), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
                for (int v = 1; v <= 10; v++)
                {
                    var pts = Enumerable.Range(0, 360).Select(k => res.TemplateToImage.Map(Target50m.Radius(v) * Math.Cos(k * Math.PI / 180), Target50m.Radius(v) * Math.Sin(k * Math.PI / 180))).ToArray();
                    using var path = new SKPath();
                    path.AddPoly(pts, true);
                    c.DrawPath(path, ring);
                }
                using var hitPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 5, IsAntialias = true };
                foreach (var hit in res.Hits) c.DrawCircle(hit.Image, 26, hitPaint);
            }

            // tile: whole photo on the left, zoom around the first hit on the right, caption at the top
            const int TH = 600, ZW = 300;
            int pw = bmp.Width * TH / bmp.Height;
            var tile = new SKBitmap(pw + ZW, TH + 60);
            using (var c = new SKCanvas(tile))
            {
                c.Clear(SKColors.White);
                c.DrawBitmap(bmp, SKRect.Create(0, 60, pw, TH));
                using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 30);
                using var black = new SKPaint { Color = SKColors.Black, IsAntialias = true };
                using var red = new SKPaint { Color = SKColors.Red, IsAntialias = true };
                string found = res.Hits.Count == 0 ? "kein Treffer" : string.Join(", ", res.Hits.Select(x => $"P10 {x.Score} / P4 {Target50m.P10ToP4(x.Score)}"));
                c.DrawText(name, 8, 40, font, black);
                c.DrawText(found, 130, 40, font, res.Hits.Count == 0 ? black : red);
                if (res.Hits.Count > 0)
                {
                    var p = res.Hits[0].Image; float z = 70;
                    c.DrawBitmap(bmp, new SKRect(p.X - z, p.Y - z * 2, p.X + z, p.Y + z * 2), SKRect.Create(pw, 60, ZW, TH));
                }
            }
            tiles.Add(tile);
            using (var data = bmp.Encode(SKEncodedImageFormat.Jpeg, 88))
                File.WriteAllBytes(Path.Combine(outDir, $"50m_{name}.jpg"), data.ToArray());
        }
        if (tiles.Count > 1)
        {
            int cols = 2, tw = tiles.Max(t => t.Width), th = tiles[0].Height, rows = (tiles.Count + cols - 1) / cols;
            using var sheet = new SKBitmap(cols * tw, rows * th);
            using (var c = new SKCanvas(sheet))
            {
                c.Clear(SKColors.White);
                for (int i = 0; i < tiles.Count; i++) c.DrawBitmap(tiles[i], i % cols * tw, i / cols * th);
            }
            using var data = sheet.Encode(SKEncodedImageFormat.Jpeg, 85);
            File.WriteAllBytes(Path.Combine(outDir, "50m_treffer.jpg"), data.ToArray());
        }
    }
}
