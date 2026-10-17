using SkiaSharp;

namespace ShootCounter.TargetScan;

public sealed record ScanResult50m(Homography TemplateToImage, double FitScore, List<Hit> Hits, bool[] Ridges, Face50m Face = Face50m.Precision)
{
    public bool IsDarkPrint(SKPoint template, double margin = 0) => Target50m.IsDarkPrint(Face, template.X, template.Y, margin);

    /// <summary>Image pixels per template unit (per mm, except on the half-size PP10 face) near the centre.</summary>
    public double PxPerMm => SKPoint.Distance(TemplateToImage.Map(0, 0), TemplateToImage.Map(100, 0)) / 100;
    public double HoleRadiusPx => PxPerMm * Target50m.CaliberMm / 2 * Target50m.UnitsPerMm(Face);
    public bool IsNearDarkLine(SKPoint template) => Target50m.IsNearDarkLine(Face, template.X, template.Y);
}

/// <summary>Finds the round 50m target (P10/P4 or B5/B10 face) in a photo and detects the new bullet holes.</summary>
/// <remarks>
/// Equally spaced circles fit almost as well one ring too large or too small, so the dark centre disc
/// (radius 100 mm on P10, 75 mm on B) gives the start position and scale; the ring lines then refine the fit.
/// Only the P10 face has been verified on real photos; the B face uses the same rules on its dark figure.
/// </remarks>
public static class Scanner50m
{
    /// <summary>Optional log of the hole candidate decisions (development).</summary>
    public static Action<string>? Log;

    /// <param name="detail">The same photo at a higher resolution (ideally full size), used to tell sharp holes from blurred glow.</param>
    public static ScanResult50m? Scan(Raster img, Raster? detail = null, Face50m face = Face50m.Precision)
    {
        int w = img.Width, h = img.Height;
        var disc = FindBlackDisc(img);
        if (disc == null) return null;
        var (cx, cy, rx, ry) = disc.Value;

        var ridges = TemplateFitting.FindRidges(img, brightLines: false);
        var nearest = TemplateFitting.NearestMap(ridges, w, h);
        // the dark ring lines outside the disc (inside it they are printed white)
        var lines = Target50m.LinePoints(4).Where(p => Math.Sqrt(p.X * p.X + p.Y * p.Y) > Target50m.DiscRadius(face) + 5).ToList();
        // start: the disc outline as an axis-aligned ellipse (the photo is often taken from below)
        double R = Target50m.DiscRadius(face);
        var H = Homography.FromPoints(
            [new(-(float)R, 0), new((float)R, 0), new(0, -(float)R), new(0, (float)R)],
            [new((float)(cx - rx), (float)cy), new((float)(cx + rx), (float)cy), new((float)cx, (float)(cy - ry)), new((float)cx, (float)(cy + ry))]);
        // Ring lines are 50 mm apart, a search radius of a third of that cannot jump to the next ring.
        // Perspective grows towards the outside, so the rings are added one by one from the inside.
        double ringPx = 50 * Math.Min(rx, ry) / R;
        for (int value = 8; value >= 1; value--)
        {
            double maxR = Target50m.Radius(value) + 1;
            var inner = lines.Where(p => p.X * p.X + p.Y * p.Y <= maxR * maxR).ToList();
            foreach (var rel in new[] { 0.35, 0.25, 0.15 })
                H = TemplateFitting.Refine(H, inner, nearest, w, h, rel * ringPx, full: value <= 7) ?? H;
        }
        foreach (var r in new[] { 10.0, 8, 6, 5, 4 })
            H = TemplateFitting.Refine(H, lines, nearest, w, h, r * w / 1200, full: true) ?? H;

        var result = new ScanResult50m(H, 0, [], ridges, face);
        result.Hits.AddRange(FindHits(img, detail ?? img, result));
        return result;
    }

    /// <summary>
    /// Open holes. On the black disc they are bright, warm spots (daylight through the hole), much brighter
    /// than the printed white line and digits. On the white paper the paper itself is backlit, so a hole is
    /// hardly brighter, but textured and golden (the background seen through it); patches over old holes are
    /// bluish and smooth, the glow of holes in the backing behind the paper is smooth.
    /// </summary>
    static List<Hit> FindHits(Raster img, Raster detail, ScanResult50m res)
    {
        var inv = res.TemplateToImage.Inverse();
        double holeR = res.HoleRadiusPx;
        var hits = new List<Hit>();
        var rejected = new List<SKPoint>();
        void Add(SKPoint p, double response, bool verify, double brighter = 0)
        {
            if (hits.Any(q => SKPoint.Distance(q.Image, p) < holeR * 1.5) || rejected.Any(q => SKPoint.Distance(q, p) < holeR)) return;
            var t = inv.Map(p);
            if (verify)
            {
                double dark = DarkFraction(img, p, holeR, inv, res), blue = BluishFraction(img, p, holeR);
                double edge = VeryDarkFraction(img, p, holeR * 2.5, inv, res);
                var (sharp, rim, elong) = EdgeFeatures(detail, img.Width, p, holeR, inv, res);
                Log?.Invoke($"cand ({p.X:0},{p.Y:0}) score={Target50m.ScoreP10(Math.Sqrt(t.X * t.X + t.Y * t.Y))} resp={response:0.0} bright={brighter:0} dark={dark:0.00} edge={edge:0.00} blue={blue:0.00} sharp={sharp:0.00} rim={rim:0} elong={elong:0.0}");
                // A dim hole (backing behind it) has a frayed dark rim; a bright hole (daylight behind it)
                // is much brighter than the paper and sharp. Rejected: printed digits, the edge of the paper,
                // patches, blurred glow, crisp ink, rims as dark as ink, paper folds.
                bool dimHole = sharp is >= 0.47 and <= 0.9 && rim is >= 15 and <= 80;
                bool brightHole = brighter > 30 && sharp is >= 0.6 and <= 1.2;
                if (dark > 0.1 || edge > 0.05 || blue > 0.12 || elong > 3 || !(dimHole || brightHole)) { rejected.Add(p); return; }
            }
            hits.Add(new Hit(p, t, response, Target50m.ScoreP10(Math.Sqrt(t.X * t.X + t.Y * t.Y))));
        }

        foreach (var b in Blobs50m.Find(img, res, 400))
        {
            var t = inv.Map(b.Image);
            // on the dark print only (shrunk by 2 mm); holes on its edge are found on the light side
            if (!res.IsDarkPrint(t, -2)) continue;
            // daylight: very bright and warm, or golden (the printed white line and digits are beige, R-B about 20)
            if (!((b.Peak > 215 && b.R - b.B > 10) || (b.Peak > 170 && b.R - b.B > 45))) continue;
            // really surrounded by dark print (the printed shapes are only known to a few mm),
            // and a hole-sized spot, not part of a printed white digit or line
            if (DarkSurround(img, b.Image, holeR * 2.2, inv, res) < 0.7 || BrightExtent(img, b.Image, holeR) > 2.6 * holeR) continue;
            Add(b.Image, b.Response, verify: false);
        }
        foreach (var c in Texture50m.Find(img, res, 400))
            if (c.tex >= 10 && c.dGray > -8 && c.ringGray > 110) Add(c.p, c.tex, verify: true, c.dGray);
        return hits;
    }

    /// <summary>
    /// Fraction of dark pixels on a circle around a spot. Points that the template places outside the
    /// dark print (a hole close to its edge) are left out.
    /// </summary>
    static double DarkSurround(Raster img, SKPoint p, double r, Homography imgToTpl, ScanResult50m res)
    {
        int dark = 0, n = 0;
        for (int k = 0; k < 48; k++)
        {
            double a = k * Math.PI / 24;
            int x = (int)(p.X + r * Math.Cos(a)), y = (int)(p.Y + r * Math.Sin(a));
            if (x < 0 || y < 0 || x >= img.Width || y >= img.Height) continue;
            if (!res.IsDarkPrint(imgToTpl.Map(x, y), -3)) continue;
            n++;
            if (img.Gray(y * img.Width + x) < 110) dark++;
        }
        // too close to the edge to judge: the template already places the spot on the dark print
        return n < 12 ? 1 : (double)dark / n;
    }

    /// <summary>
    /// Largest extent (px) of the bright region around a spot: a hole is about one hole diameter,
    /// printed white digits and lines are larger. Returns a large value if the region leaves the window.
    /// </summary>
    static double BrightExtent(Raster img, SKPoint p, double holeR)
    {
        int w = img.Width, h = img.Height, cx = (int)p.X, cy = (int)p.Y, win = (int)Math.Ceiling(holeR * 4);
        int peak = 0, bg = 255;
        for (int y = Math.Max(0, cy - win); y <= Math.Min(h - 1, cy + win); y++)
            for (int x = Math.Max(0, cx - win); x <= Math.Min(w - 1, cx + win); x++)
            {
                int g = img.Gray(y * w + x);
                if (Math.Abs(x - cx) <= holeR && Math.Abs(y - cy) <= holeR) peak = Math.Max(peak, g);
                bg = Math.Min(bg, g);
            }
        int threshold = (peak + bg) / 2;
        var seen = new HashSet<int>();
        var stack = new Stack<(int x, int y)>();
        // start at the brightest pixel near the centre
        int sx = cx, sy = cy;
        for (int y = cy - (int)holeR; y <= cy + (int)holeR; y++)
            for (int x = cx - (int)holeR; x <= cx + (int)holeR; x++)
                if (x >= 0 && y >= 0 && x < w && y < h && img.Gray(y * w + x) == peak) { sx = x; sy = y; }
        stack.Push((sx, sy));
        int minX = sx, maxX = sx, minY = sy, maxY = sy;
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            if (x < 0 || y < 0 || x >= w || y >= h || !seen.Add(y * w + x) || img.Gray(y * w + x) < threshold) continue;
            if (Math.Abs(x - cx) > win || Math.Abs(y - cy) > win) return double.MaxValue;
            minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            stack.Push((x + 1, y)); stack.Push((x - 1, y)); stack.Push((x, y + 1)); stack.Push((x, y - 1));
        }
        return Math.Max(maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// Fraction of nearly black pixels (outside the dark print and the ring lines) around a spot:
    /// the dark wooden frame or background next to the edge of the paper.
    /// </summary>
    static double VeryDarkFraction(Raster img, SKPoint p, double r, Homography imgToTpl, ScanResult50m res)
    {
        int w = img.Width, h = img.Height, cx = (int)p.X, cy = (int)p.Y, ri = (int)Math.Ceiling(r), dark = 0, n = 0;
        for (int y = Math.Max(0, cy - ri); y <= Math.Min(h - 1, cy + ri); y++)
            for (int x = Math.Max(0, cx - ri); x <= Math.Min(w - 1, cx + ri); x++)
            {
                var t = imgToTpl.Map(x, y);
                double d = Math.Sqrt(t.X * t.X + t.Y * t.Y);
                if (res.IsDarkPrint(t, 3) || res.IsNearDarkLine(t)) continue;
                n++;
                if (img.Gray(y * w + x) < 75) dark++;
            }
        return n == 0 ? 0 : (double)dark / n;
    }

    /// <summary>
    /// Fraction of clearly dark pixels around a spot, leaving out the ring lines: printed digits have thick
    /// dark strokes, a hole at most a thin dark rim.
    /// </summary>
    static double DarkFraction(Raster img, SKPoint p, double holeR, Homography imgToTpl, ScanResult50m res)
    {
        int w = img.Width, h = img.Height, cx = (int)p.X, cy = (int)p.Y;
        int r = (int)Math.Ceiling(holeR * 1.5), rPaper = (int)Math.Ceiling(holeR * 3.5);
        var around = new List<int>();
        for (int y = Math.Max(0, cy - rPaper); y < Math.Min(h, cy + rPaper); y += 2)
            for (int x = Math.Max(0, cx - rPaper); x < Math.Min(w, cx + rPaper); x += 2) around.Add(img.Gray(y * w + x));
        around.Sort();
        int paper = around[around.Count / 2], dark = 0, n = 0;
        for (int y = Math.Max(0, cy - r); y <= Math.Min(h - 1, cy + r); y++)
            for (int x = Math.Max(0, cx - r); x <= Math.Min(w - 1, cx + r); x++)
            {
                var t = imgToTpl.Map(x, y);
                double d = Math.Sqrt(t.X * t.X + t.Y * t.Y);
                if (res.IsDarkPrint(t, 2) || res.IsNearDarkLine(t)) continue;
                n++;
                if (img.Gray(y * w + x) < paper - 70) dark++;
            }
        return n == 0 ? 1 : (double)dark / n;
    }

    /// <summary>Fraction of bluish pixels around a spot: the patches over old holes are bluish white.</summary>
    static double BluishFraction(Raster img, SKPoint p, double holeR)
    {
        int w = img.Width, h = img.Height, cx = (int)p.X, cy = (int)p.Y, r = (int)Math.Ceiling(holeR * 1.2), blue = 0, n = 0;
        for (int y = Math.Max(0, cy - r); y <= Math.Min(h - 1, cy + r); y++)
            for (int x = Math.Max(0, cx - r); x <= Math.Min(w - 1, cx + r); x++)
            {
                int i = y * w + x;
                n++;
                if (img.B[i] - img.R[i] > 8) blue++;
            }
        return n == 0 ? 0 : (double)blue / n;
    }

    /// <summary>
    /// Edge features of a spot, measured on the detail image, leaving out the printed ring lines.
    /// Sharpness: a high gradient percentile relative to the local contrast; the frayed edge of a hole is
    /// sharp (about 0.5 to 1), glow and patches are blurred (about 0.4).
    /// Rim: how much darker than the paper the darkest part of the ring around the spot is; the torn paper
    /// edge of a hole casts a dark rim (above 20), patches and glow have none (below 10).
    /// Elongation: ratio of the principal axes of the pixels that clearly differ from the paper; a hole is
    /// round (below 2), a paper fold is a long bright crease with a shadow.
    /// </summary>
    static (double sharp, double rim, double elong) EdgeFeatures(Raster detail, int analysisWidth, SKPoint p, double holeR, Homography imgToTpl, ScanResult50m res)
    {
        double s = detail.Width / (double)analysisWidth, hr = holeR * s;
        int w = detail.Width, h = detail.Height, cx = (int)(p.X * s), cy = (int)(p.Y * s), r = (int)Math.Ceiling(hr * 3);
        var grays = new List<int>(); var grads = new List<int>(); var annulus = new List<int>(); var paper = new List<int>();
        for (int y = Math.Max(1, cy - r); y <= Math.Min(h - 2, cy + r); y++)
            for (int x = Math.Max(1, cx - r); x <= Math.Min(w - 2, cx + r); x++)
            {
                var t = imgToTpl.Map(x / s, y / s);
                double d = Math.Sqrt(t.X * t.X + t.Y * t.Y);
                if (res.IsDarkPrint(t, 2) || res.IsNearDarkLine(t)) continue;
                int i = y * w + x, g = detail.Gray(i);
                double rel = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / hr;
                if (rel < 0.9)
                {
                    grays.Add(g);
                    grads.Add(Math.Abs(detail.Gray(i + 1) - detail.Gray(i - 1)) + Math.Abs(detail.Gray(i + w) - detail.Gray(i - w)));
                }
                if (rel >= 0.6 && rel < 1.4) annulus.Add(g);
                else if (rel >= 2 && rel < 3) paper.Add(g);
            }
        if (grays.Count < 20 || annulus.Count < 20 || paper.Count < 20) return (0, 0, 0);
        grays.Sort(); grads.Sort(); annulus.Sort(); paper.Sort();
        int paperLevel = paper[paper.Count / 2];
        double rim = paperLevel - annulus[annulus.Count / 20];

        // shape of the pixels that differ clearly from the paper, within the hole and its rim
        double n = 0, mx = 0, my = 0, sxx = 0, syy = 0, sxy = 0;
        int rr = (int)Math.Ceiling(hr * 1.4);
        for (int y = Math.Max(0, cy - rr); y <= Math.Min(h - 1, cy + rr); y++)
            for (int x = Math.Max(0, cx - rr); x <= Math.Min(w - 1, cx + rr); x++)
            {
                if (Math.Abs(detail.Gray(y * w + x) - paperLevel) < 25) continue;
                var t = imgToTpl.Map(x / s, y / s);
                double d = Math.Sqrt(t.X * t.X + t.Y * t.Y);
                if (res.IsDarkPrint(t, 2) || res.IsNearDarkLine(t)) continue;
                n++; mx += x; my += y; sxx += x * (double)x; syy += y * (double)y; sxy += x * (double)y;
            }
        double elong = 0;
        if (n > 10)
        {
            mx /= n; my /= n;
            double cxx = sxx / n - mx * mx, cyy = syy / n - my * my, cxy = sxy / n - mx * my;
            double tr = cxx + cyy, det = cxx * cyy - cxy * cxy, disc = Math.Sqrt(Math.Max(0, tr * tr / 4 - det));
            double l1 = tr / 2 + disc, l2 = Math.Max(1e-6, tr / 2 - disc);
            elong = Math.Sqrt(l1 / l2);
        }

        double contrast = grays[(int)(grays.Count * 0.98)] - grays[(int)(grays.Count * 0.10)];
        if (contrast < 35) return (0, rim, elong);
        return (grads[(int)(grads.Count * 0.95)] / contrast, rim, elong);
    }

    /// <summary>Centre and half-axes (px) of the black disc: the largest dark, roughly round blob.</summary>
    static (double cx, double cy, double rx, double ry)? FindBlackDisc(Raster img)
    {
        int w = img.Width, h = img.Height;
        var dark = new bool[w * h];
        for (int i = 0; i < dark.Length; i++) dark[i] = img.Gray(i) < 80;
        // close the white 10 line, digits and bright holes inside the disc
        int k = Math.Max(2, w / 200);
        var closed = Mask.Erode(Mask.Dilate(dark, w, h, k), w, h, k);
        var (_, comps) = Mask.Components(closed, w, h);
        var disc = comps
            .Where(c => c.BoxW > w / 15 && c.BoxH > w / 15 && c.MinY > 0 && c.MaxY < h - 1)
            .Where(c => Math.Abs(c.BoxW - c.BoxH) < 0.25 * Math.Max(c.BoxW, c.BoxH))   // round
            .Where(c => c.Area > 0.6 * Math.PI * c.BoxW * c.BoxH / 4)                     // filled
            .MaxBy(c => c.Area);
        if (disc == null) return null;
        return ((disc.MinX + disc.MaxX) / 2.0, (disc.MinY + disc.MaxY) / 2.0, disc.BoxW / 2.0, disc.BoxH / 2.0);
    }
}

/// <summary>Bright blob candidate with the features used to tell open holes from patches and glow.</summary>
public sealed record BlobCandidate(SKPoint Image, double Response, double Peak, double R, double G, double B, double Edge, int Score);

public static class Blobs50m
{
    /// <summary>Bright blobs of hole size on the target, strongest first (dev/analysis helper).</summary>
    public static List<BlobCandidate> Find(Raster img, ScanResult50m res, int max)
    {
        int w = img.Width, h = img.Height;
        var inv = res.TemplateToImage.Inverse();
        double holeR = res.HoleRadiusPx;
        var ii = new long[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            long row = 0;
            for (int x = 0; x < w; x++) { row += img.Gray(y * w + x); ii[(y + 1) * (w + 1) + x + 1] = ii[y * (w + 1) + x + 1] + row; }
        }
        long Box(int x0, int y0, int x1, int y1) => ii[y1 * (w + 1) + x1] - ii[y0 * (w + 1) + x1] - ii[y1 * (w + 1) + x0] + ii[y0 * (w + 1) + x0];
        int rIn = Math.Max(1, (int)Math.Round(holeR * 0.6)), rMid = (int)Math.Ceiling(holeR * 1.5), rOut = (int)Math.Ceiling(holeR * 2.5);
        double nIn = (2 * rIn + 1) * (2 * rIn + 1), nRing = (2 * rOut + 1) * (2 * rOut + 1) - (2 * rMid + 1) * (2 * rMid + 1);
        var cands = new System.Collections.Concurrent.ConcurrentBag<(double resp, int x, int y)>();
        Parallel.For(rOut, h - rOut - 1, y =>
        {
            for (int x = rOut; x < w - rOut - 1; x++)
            {
                double inner = Box(x - rIn, y - rIn, x + rIn + 1, y + rIn + 1) / nIn;
                double outer = (Box(x - rOut, y - rOut, x + rOut + 1, y + rOut + 1) - Box(x - rMid, y - rMid, x + rMid + 1, y + rMid + 1)) / nRing;
                double resp = inner - outer;
                if (resp < 15) continue;
                var t = inv.Map(x, y);
                if (t.X * t.X + t.Y * t.Y > 505 * 505) continue;
                cands.Add((resp, x, y));
            }
        });
        var result = new List<BlobCandidate>();
        foreach (var (resp, x, y) in cands.OrderByDescending(c => c.resp))
        {
            if (result.Any(q => SKPoint.Distance(q.Image, new SKPoint(x, y)) < holeR * 2)) continue;
            // colour of the brightest pixels of the core, and the edge sharpness (max gradient around the core)
            var core = new List<int>();
            for (int yy = y - rIn; yy <= y + rIn; yy++)
                for (int xx = x - rIn; xx <= x + rIn; xx++) core.Add(yy * w + xx);
            var top = core.OrderByDescending(img.Gray).Take(Math.Max(3, core.Count / 4)).ToList();
            double edge = 0;
            for (int yy = y - rMid; yy <= y + rMid; yy++)
                for (int xx = x - rMid; xx <= x + rMid; xx++)
                {
                    int i = yy * w + xx;
                    edge = Math.Max(edge, Math.Abs(img.Gray(i + 1) - img.Gray(i - 1)) + Math.Abs(img.Gray(i + w) - img.Gray(i - w)));
                }
            var t = inv.Map(x, y);
            result.Add(new BlobCandidate(new SKPoint(x, y), resp, top.Average(i => (double)img.Gray(i)),
                top.Average(i => (double)img.R[i]), top.Average(i => (double)img.G[i]), top.Average(i => (double)img.B[i]), edge,
                Target50m.ScoreP10(Math.Sqrt(t.X * t.X + t.Y * t.Y))));
            if (result.Count >= max) break;
        }
        return result;
    }
}

public static class Texture50m
{
    /// <summary>
    /// Hole-sized spots in the white area that are textured (std. deviation of non-line pixels) while their
    /// surroundings are smooth and that are not much brighter than the paper (unlike patches), strongest first.
    /// </summary>
    public static List<(SKPoint p, double tex, double dGray, double peak, int score, double warm, double ringGray)> Find(Raster img, ScanResult50m res, int max)
    {
        int w = img.Width, h = img.Height;
        var inv = res.TemplateToImage.Inverse();
        double holeR = res.HoleRadiusPx;
        // printed lines: detected ridges, plus everything close to the fitted ring circles
        var lines = Mask.Dilate(res.Ridges, w, h, 1);
        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                var t = inv.Map(x, y);
                if (res.IsNearDarkLine(t)) lines[y * w + x] = true;
            }
        });
        // integral images of count, sum and square sum over non-line pixels
        var sn = new double[(w + 1) * (h + 1)];
        var s1 = new double[(w + 1) * (h + 1)];
        var s2 = new double[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            double rn = 0, r1 = 0, r2 = 0;
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (!lines[i]) { double g = img.Gray(i); rn++; r1 += g; r2 += g * g; }
                int k = (y + 1) * (w + 1) + x + 1;
                sn[k] = sn[k - w - 1] + rn; s1[k] = s1[k - w - 1] + r1; s2[k] = s2[k - w - 1] + r2;
            }
        }
        double Box(double[] s, int x0, int y0, int x1, int y1) => s[y1 * (w + 1) + x1] - s[y0 * (w + 1) + x1] - s[y1 * (w + 1) + x0] + s[y0 * (w + 1) + x0];
        (double n, double m, double sd) Stats(int x0, int y0, int x1, int y1)
        {
            double n = Box(sn, x0, y0, x1, y1);
            if (n < 4) return (n, 0, 0);
            double m = Box(s1, x0, y0, x1, y1) / n;
            return (n, m, Math.Sqrt(Math.Max(0, Box(s2, x0, y0, x1, y1) / n - m * m)));
        }
        int rIn = Math.Max(2, (int)Math.Round(holeR * 0.8)), rOut = (int)Math.Ceiling(holeR * 2.2);
        var cands = new System.Collections.Concurrent.ConcurrentBag<(double tex, int x, int y, double dGray)>();
        Parallel.For(rOut, h - rOut - 1, y =>
        {
            for (int x = rOut; x < w - rOut - 1; x++)
            {
                var a = Stats(x - rIn, y - rIn, x + rIn + 1, y + rIn + 1);
                if (a.n < 0.5 * (2 * rIn + 1) * (2 * rIn + 1) || a.sd < 12) continue;
                var outer = Stats(x - rOut, y - rOut, x + rOut + 1, y + rOut + 1);
                double nRing = outer.n - a.n;
                if (nRing < 10) continue;
                double mRing = (outer.m * outer.n - a.m * a.n) / nRing;
                double vRing = (outer.n * (outer.sd * outer.sd + outer.m * outer.m) - a.n * (a.sd * a.sd + a.m * a.m)) / nRing - mRing * mRing;
                double tex = a.sd - Math.Sqrt(Math.Max(0, vRing));
                if (tex < 8) continue;
                var t = inv.Map(x, y);
                double d = Math.Sqrt(t.X * t.X + t.Y * t.Y);
                if (res.IsDarkPrint(t, Target50m.CaliberMm * Target50m.UnitsPerMm(res.Face)) || d > 505) continue;
                cands.Add((tex, x, y, a.m - mRing));
            }
        });
        var result = new List<(SKPoint, double, double, double, int, double, double)>();
        foreach (var c in cands.OrderByDescending(c => c.tex))
        {
            if (result.Any(q => SKPoint.Distance(q.Item1, new SKPoint(c.x, c.y)) < holeR * 2)) continue;
            // warmth (R - B) of the spot relative to its surroundings: holes are golden, patches bluish
            double peak = 0, warmIn = 0, warmOut = 0, grayOut = 0; int nIn = 0, nOut = 0;
            for (int yy = c.y - rOut; yy <= c.y + rOut; yy++)
                for (int xx = c.x - rOut; xx <= c.x + rOut; xx++)
                {
                    int i = yy * w + xx;
                    if (Math.Abs(xx - c.x) <= rIn && Math.Abs(yy - c.y) <= rIn) { peak = Math.Max(peak, img.Gray(i)); warmIn += img.R[i] - img.B[i]; nIn++; }
                    else if (!lines[i]) { warmOut += img.R[i] - img.B[i]; grayOut += img.Gray(i); nOut++; }
                }
            var t = inv.Map(c.x, c.y);
            result.Add((new SKPoint(c.x, c.y), c.tex, c.dGray, peak, Target50m.ScoreP10(Math.Sqrt(t.X * t.X + t.Y * t.Y)),
                warmIn / Math.Max(1, nIn) - warmOut / Math.Max(1, nOut), grayOut / Math.Max(1, nOut)));
            if (result.Count >= max) break;
        }
        return result;
    }
}
