using SkiaSharp;
using ShootCounter.TargetScan;

if (args[0] == "zoom") { using var full = SKBitmap.Decode(args[1]); int zx = int.Parse(args[2]), zy = int.Parse(args[3]), zr = int.Parse(args[4]); using var zb = new SKBitmap(800, 800); using (var zc = new SKCanvas(zb)) zc.DrawBitmap(full, SKRect.Create(zx - zr, zy - zr, 2 * zr, 2 * zr), new SKRect(0, 0, 800, 800)); Save(zb, args[5]); return; }
if (args[0] == "rim50") { var rl = new List<(int, int)>(); for (int a = 3; a + 1 < args.Length; a += 2) rl.Add((int.Parse(args[a]), int.Parse(args[a + 1]))); Rim50.Run(args[1], double.Parse(args[2]), rl.ToArray()); return; }
if (args[0] == "synthm") { SynthM.Run(args[1], Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out"))); return; }
if (args[0] == "figm") { FigureM.Run(args[1]); return; }
if (args[0] == "synthb") { SynthB.Run(args[1], Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out"))); return; }
if (args[0] == "figb") { FigureB.Run(args[1]); return; }
if (args[0] == "hits50") { Hits50.Run(args[1]); return; }
if (args[0] == "near50") { Near50.Run(args[1], int.Parse(args[2]), int.Parse(args[3])); return; }
if (args[0] == "dark50") { var dl = new List<(int, int)>(); for (int a = 2; a + 1 < args.Length; a += 2) dl.Add((int.Parse(args[a]), int.Parse(args[a + 1]))); Dark50.Run(args[1], dl.ToArray()); return; }
if (args[0] == "detail50") { var pl = new List<(int, int)>(); for (int a = 2; a + 1 < args.Length; a += 2) pl.Add((int.Parse(args[a]), int.Parse(args[a + 1]))); Detail50.Run(args[1], pl.ToArray()); return; }
if (args[0] == "sharp50") { for (int a = 2; a + 1 < args.Length; a += 2) Sharp50.Run(args[1], int.Parse(args[a]), int.Parse(args[a + 1])); return; }
if (args[0] == "probe50") { Probe50.Run(args[1], int.Parse(args[2]), int.Parse(args[3])); return; }
if (args[0] == "scan50") { Scan50.Run(args[1], Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out")), args.Length > 2 ? args[2] : null); return; }
if (args[0] == "overview") { var of = Directory.GetFiles(args[1], "PXL_*.jpg").OrderBy(f => f).Take(10).ToArray(); using var ov = new SKBitmap(5 * 600, 2 * 800); using (var oc = new SKCanvas(ov)) { using var of2 = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 40); using var op = new SKPaint { Color = SKColors.Red }; for (int i = 0; i < of.Length; i++) { using var st = File.OpenRead(of[i]); using var b = Raster.LoadBitmap(st, 800); var rc = SKRect.Create(i % 5 * 600, i / 5 * 800, 600, 800); oc.DrawBitmap(b, rc); oc.DrawText(Path.GetFileNameWithoutExtension(of[i])[13..19], rc.Left + 10, rc.Top + 45, of2, op); } } Save(ov, args[2]); return; }
if (args[0] == "radial") { RadialProfile.Run(args[1]); return; }
if (args[0] == "template") { TemplateFit.Run(args[1]); AxisProfile.Run(args[1]); return; }
var dir = args[0];
var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out"));
Directory.CreateDirectory(outDir);
var files = Directory.GetFiles(dir, "PXL_*.jpg").OrderBy(f => f).Skip(10).ToArray();
if (args.Length > 1) files = files.Where(f => f.Contains(args[1])).ToArray();


if (Environment.GetEnvironmentVariable("SCAN_MARGIN") == "1") foreach (var f in files) { var r = Scanner.Scan(Raster.Load(f, 1600))!; Console.WriteLine(Path.GetFileName(f) + ": " + string.Join(" ", r.Hits.Select(x => { var ring = r.Rings.First(q => q.value == (x.Score == 10 ? 10 : x.Score + 1 > 10 ? 10 : x.Score + 1)); double d = Target25m.StadiumDist(x.Template.X, x.Template.Y, ring.a, ring.b) - Target25m.CaliberMm / 2; return $"{x.Score}(+{d:0.0}mm)"; }))); }
var annotated = new List<(string name, SKBitmap bmp)>();
foreach (var f in files)
{
    var name = Path.GetFileNameWithoutExtension(f)[13..19];
    var img = Raster.Load(f, 1600);
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var res = Scanner.Scan(img);
    sw.Stop();
    if (res == null) { Console.WriteLine($"{name}: no target"); continue; }
    var scores = res.Hits.OrderByDescending(x => x.Score).Select(x => $"{x.Score}{(x.Uncertain ? "?" : "")}{(x.Borderline ? "~" : "")}").ToList();
    var total = res.Hits.Sum(x => x.Score);
    Console.WriteLine($"   rings: {string.Join(" ", res.Rings.Select(r => $"{r.value}:{r.a:0.0}x{r.b:0.0}"))}");
    Console.WriteLine($"{name}: fit={res.FitScore:0.00} {sw.ElapsedMilliseconds}ms hits={res.Hits.Count} total={total} [{string.Join(",", scores)}]");
    var bmp = Render(img, res, $"{name} fit={res.FitScore:0.00} n={res.Hits.Count} sum={total}");
    Save(bmp, Path.Combine(outDir, $"{name}.jpg"));
    annotated.Add((name, Crops.CentreCrop(bmp, res, name)));
}
if (annotated.Count > 1) Save(Sheet(annotated, 6), Path.Combine(outDir, "sheet.jpg"));

static SKBitmap Render(Raster img, ScanResult res, string title)
{
    var bmp = img.ToBitmap();
    using var c = new SKCanvas(bmp);
    using var ridge = new SKPaint { Color = new SKColor(255, 0, 255, 90) };
    for (int i = 0; i < res.Ridges.Length; i++)
        if (res.Ridges[i]) c.DrawPoint(i % img.Width, i / img.Width, ridge);
    using var ring = new SKPaint { Color = SKColors.Lime, Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true };
    foreach (var r in res.Rings)
    {
        var pts = Target25m.StadiumOutline(r.a, r.b, 4).Select(res.TemplateToImage.Map).ToArray();
        using var path = new SKPath();
        path.AddPoly(pts, true);
        c.DrawPath(path, ring);
    }
    using var hitPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 3, IsAntialias = true };
    using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 26);
    using var text = new SKPaint { Color = SKColors.Yellow, IsAntialias = true };
    foreach (var hit in res.Hits)
    {
        hitPaint.Color = hit.Uncertain ? SKColors.Orange : SKColors.Red;
        c.DrawCircle(hit.Image, 9, hitPaint);
        c.DrawText($"{hit.Score}{(hit.Uncertain ? "?" : "")} ({hit.Response:0})", hit.Image.X + 16, hit.Image.Y - 10, font, text);
    }
    using var bg = new SKPaint { Color = new SKColor(0, 0, 0, 180) };
    c.DrawRect(0, 0, img.Width, 50, bg);
    c.DrawText(title, 10, 38, font, text);
    return bmp;
}

static SKBitmap Sheet(List<(string name, SKBitmap bmp)> items, int cols)
{
    int tw = items[0].bmp.Width / 2, th = items[0].bmp.Height / 2, rows = (items.Count + cols - 1) / cols;
    var sheet = new SKBitmap(cols * tw, rows * th);
    using var c = new SKCanvas(sheet);
    c.Clear(SKColors.Gray);
    for (int i = 0; i < items.Count; i++)
        c.DrawBitmap(items[i].bmp, SKRect.Create(i % cols * tw, i / cols * th, tw, th));
    return sheet;
}

static void Save(SKBitmap bmp, string path)
{
    using var data = bmp.Encode(SKEncodedImageFormat.Jpeg, 85);
    File.WriteAllBytes(path, data.ToArray());
}
