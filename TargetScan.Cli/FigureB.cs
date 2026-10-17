using ShootCounter.TargetScan;
using SkiaSharp;

/// <summary>Dev tool: colour runs along rows/columns through the B5/B10 reference image.</summary>
static class FigureB
{
    public static void Run(string path)
    {
        using var bmp = SKBitmap.Decode(path);
        var r = Raster.FromBitmap(bmp);
        int w = r.Width, h = r.Height;
        char Kind(int i) { int g = r.Gray(i); return g < 90 ? 'D' : g < 150 ? 'M' : g < 185 ? 'L' : '.'; }
        Console.WriteLine($"{w}x{h}");
        void Runs(string label, IEnumerable<int> idx)
        {
            var s = idx.Select(Kind).ToArray();
            var parts = new List<string>();
            int start = 0;
            for (int i = 1; i <= s.Length; i++)
                if (i == s.Length || s[i] != s[start])
                {
                    if (i - start >= 3) parts.Add($"{s[start]}[{start}-{i - 1}]");
                    start = i;
                }
            Console.WriteLine($"{label}: {string.Join(" ", parts)}");
        }
        foreach (int y in new[] { 243, 200, 160, 120, 300, 330 })
            Runs($"row {y}", Enumerable.Range(0, w).Select(x => y * w + x));
        foreach (int x in new[] { 249, 170, 140, 220, 290 })
            Runs($"col {x}", Enumerable.Range(0, h).Select(y => y * w + x));
    }
}
