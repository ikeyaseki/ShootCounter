using ShootCounter.TargetScan;

static class Probe50
{
    /// <summary>Dev tool: local features at a point of the analysed (1600 px) image.</summary>
    public static void Run(string path, int cx, int cy)
    {
        var img = Raster.Load(path, 1600);
        int w = img.Width;
        foreach (int r in new[] { 3, 6, 10 })
        {
            var px = new List<int>();
            for (int y = cy - r; y <= cy + r; y++) for (int x = cx - r; x <= cx + r; x++) px.Add(y * w + x);
            double m = px.Average(i => (double)img.Gray(i)), sd = Math.Sqrt(px.Average(i => Math.Pow(img.Gray(i) - m, 2)));
            double sat = px.Average(i => (double)(Math.Max(img.R[i], Math.Max(img.G[i], img.B[i])) - Math.Min(img.R[i], Math.Min(img.G[i], img.B[i]))));
            Console.WriteLine($"r={r}: gray={m:0} sd={sd:0.0} sat={sat:0.0} rgb=({px.Average(i => (double)img.R[i]):0},{px.Average(i => (double)img.G[i]):0},{px.Average(i => (double)img.B[i]):0})");
        }
        // ring around
        var ring = new List<int>();
        for (int y = cy - 22; y <= cy + 22; y++) for (int x = cx - 22; x <= cx + 22; x++) { int d = Math.Max(Math.Abs(x - cx), Math.Abs(y - cy)); if (d >= 14) ring.Add(y * w + x); }
        double rm = ring.Average(i => (double)img.Gray(i));
        Console.WriteLine($"ring: gray={rm:0} sd={Math.Sqrt(ring.Average(i => Math.Pow(img.Gray(i) - rm, 2))):0.0} sat={ring.Average(i => (double)(Math.Max(img.R[i], Math.Max(img.G[i], img.B[i])) - Math.Min(img.R[i], Math.Min(img.G[i], img.B[i])))):0.0}");
    }
}

static class Sharp50
{
    /// <summary>Max gradient (|dx|+|dy| over 1 px) and mean colour around a point.</summary>
    public static void Run(string path, int cx, int cy)
    {
        var img = Raster.Load(path, 1600);
        int w = img.Width;
        double edge = 0; double sat = 0; int n = 0; double rr = 0, gg = 0, bb = 0;
        for (int y = cy - 8; y <= cy + 8; y++)
            for (int x = cx - 8; x <= cx + 8; x++)
            {
                int i = y * w + x;
                edge = Math.Max(edge, Math.Abs(img.Gray(i + 1) - img.Gray(i - 1)) + Math.Abs(img.Gray(i + w) - img.Gray(i - w)));
                sat = Math.Max(sat, Math.Max(img.R[i], Math.Max(img.G[i], img.B[i])) - Math.Min(img.R[i], Math.Min(img.G[i], img.B[i])));
                if (Math.Abs(x - cx) <= 4 && Math.Abs(y - cy) <= 4) { rr += img.R[i]; gg += img.G[i]; bb += img.B[i]; n++; }
            }
        Console.WriteLine($"({cx},{cy}) edge={edge} maxSat={sat} core rgb=({rr / n:0},{gg / n:0},{bb / n:0})");
    }
}

static class Detail50
{
    /// <summary>Sharpness at full resolution: high gradient percentile relative to the local contrast.</summary>
    public static void Run(string path, (int x, int y)[] pts)
    {
        var full = Raster.Load(path, 99999);
        double s = full.Width / (double)Raster.Load(path, 1600).Width;
        int w = full.Width;
        foreach (var (px, py) in pts)
        {
            int cx = (int)(px * s), cy = (int)(py * s), r = (int)(14 * s / 2.5 * 1.0);
            var grays = new List<int>(); var grads = new List<int>();
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    int i = y * w + x;
                    grays.Add(full.Gray(i));
                    grads.Add(Math.Abs(full.Gray(i + 1) - full.Gray(i - 1)) + Math.Abs(full.Gray(i + w) - full.Gray(i - w)));
                }
            grays.Sort(); grads.Sort();
            double contrast = grays[(int)(grays.Count * 0.98)] - grays[(int)(grays.Count * 0.10)];
            double g95 = grads[(int)(grads.Count * 0.95)], g99 = grads[(int)(grads.Count * 0.99)];
            Console.WriteLine($"({px},{py}) r={r} contrast={contrast} g95={g95} g99={g99} g95/c={g95 / Math.Max(1, contrast):0.00} g99/c={g99 / Math.Max(1, contrast):0.00}");
        }
    }
}

static class Dark50
{
    public static void Run(string path, (int x, int y)[] pts)
    {
        var img = Raster.Load(path, 1600);
        int w = img.Width;
        foreach (var (cx, cy) in pts)
        {
            int r = 13, dark = 0, n = 0; var g = new List<int>();
            for (int y = cy - 30; y <= cy + 30; y++) for (int x = cx - 30; x <= cx + 30; x++) g.Add(img.Gray(y * w + x));
            g.Sort(); int paper = g[g.Count / 2];
            for (int y = cy - r; y <= cy + r; y++) for (int x = cx - r; x <= cx + r; x++) { n++; if (img.Gray(y * w + x) < paper - 70) dark++; }
            Console.WriteLine($"({cx},{cy}) paper={paper} darkFrac={(double)dark / n:0.000}");
        }
    }
}

static class Rim50
{
    /// <summary>Dark rim: paper level minus the 5th percentile in an annulus around the spot (full resolution).</summary>
    public static void Run(string path, double holeRAnalysis, (int x, int y)[] pts)
    {
        var full = Raster.Load(path, 99999);
        double s = full.Width / (double)Raster.Load(path, 1600).Width;
        int w = full.Width;
        double hr = holeRAnalysis * s;
        foreach (var (px, py) in pts)
        {
            int cx = (int)(px * s), cy = (int)(py * s);
            var ann = new List<int>(); var paper = new List<int>(); var core = new List<int>();
            int R = (int)(hr * 3);
            for (int y = cy - R; y <= cy + R; y++)
                for (int x = cx - R; x <= cx + R; x++)
                {
                    double d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / hr;
                    int g = full.Gray(y * w + x);
                    if (d < 0.6) core.Add(g); else if (d >= 0.6 && d < 1.4) ann.Add(g); else if (d >= 2 && d < 3) paper.Add(g);
                }
            ann.Sort(); paper.Sort(); core.Sort();
            int pap = paper[paper.Count / 2];
            Console.WriteLine($"({px},{py}) paper={pap} core={core[core.Count / 2]} ann p5={ann[ann.Count / 20]} p10={ann[ann.Count / 10]} rim={pap - ann[ann.Count / 20]} rim10={pap - ann[ann.Count / 10]}");
        }
    }
}
