using ShootCounter.TargetScan;
using SkiaSharp;

/// <summary>Dev tool: green figure outline and white ring lines of the Morgarten reference image.</summary>
static class FigureM
{
    public static void Run(string path)
    {
        using var bmp = SKBitmap.Decode(path);
        var r = Raster.FromBitmap(bmp);
        int w = r.Width, h = r.Height;
        bool Green(int i) => r.G[i] > r.R[i] + 25 && r.G[i] > r.B[i] + 10;
        bool Line(int i) => r.Gray(i) > 170 && Green(i - 2) && Green(i + 2) == false ? false : r.Gray(i) > 160 && r.G[i] - r.R[i] < 25;
        Console.WriteLine($"{w}x{h}");
        // figure extent per row
        for (int y = 0; y < h; y += h / 40)
        {
            int l = -1, rr = -1;
            for (int x = 0; x < w; x++) if (Green(y * w + x)) { if (l < 0) l = x; rr = x; }
            Console.WriteLine($"row {y}: green {l}..{rr}");
        }
        // white lines along the vertical centre column and the horizontal centre row through the ring centre
        int cx = w / 2;
        var vy = Enumerable.Range(2, h - 4).Where(y => { int i = y * w + cx; return r.Gray(i) > r.Gray(i - 2 * w) + 25 && r.Gray(i) > r.Gray(i + 2 * w) + 25; }).ToList();
        Console.WriteLine("col centre lines y: " + string.Join(" ", vy));
        int cy = h * 470 / 800;
        var hx = Enumerable.Range(2, w - 4).Where(x => { int i = cy * w + x; return r.Gray(i) > r.Gray(i - 2) + 25 && r.Gray(i) > r.Gray(i + 2) + 25; }).ToList();
        Console.WriteLine($"row {cy} lines x: " + string.Join(" ", hx));
    }
}
