using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>3x3 projective transform, maps (x,y) -> (x',y').</summary>
public sealed class Homography
{
    readonly double[] h; // row-major, h[8] = 1

    Homography(double[] h) => this.h = h;

    public SKPoint Map(double x, double y)
    {
        double w = h[6] * x + h[7] * y + h[8];
        return new SKPoint((float)((h[0] * x + h[1] * y + h[2]) / w), (float)((h[3] * x + h[4] * y + h[5]) / w));
    }

    public SKPoint Map(SKPoint p) => Map(p.X, p.Y);

    /// <summary>Similarity transform: scale s, rotation (radians), then translation.</summary>
    public static Homography Similarity(double s, double angle, double tx, double ty)
    {
        double c = s * Math.Cos(angle), si = s * Math.Sin(angle);
        return new Homography([c, -si, tx, si, c, ty, 0, 0, 1]);
    }

    /// <summary>Weighted least-squares homography from n >= 4 point pairs (normalized DLT with h22 = 1).</summary>
    public static Homography FromPoints(IReadOnlyList<SKPoint> src, IReadOnlyList<SKPoint> dst, IReadOnlyList<double>? weights = null)
    {
        var (ns, nsInv) = Normalizer(src);
        var (nd, ndInv) = Normalizer(dst);
        var ata = new double[8, 8];
        var atb = new double[8];
        for (int i = 0; i < src.Count; i++)
        {
            var s = ns.Map(src[i]); var d = nd.Map(dst[i]);
            double x = s.X, y = s.Y, u = d.X, v = d.Y, w = weights?[i] ?? 1;
            if (w <= 0) continue;
            Accumulate(ata, atb, [x, y, 1, 0, 0, 0, -u * x, -u * y], u, w);
            Accumulate(ata, atb, [0, 0, 0, x, y, 1, -v * x, -v * y], v, w);
        }
        var hn = new Homography([.. Solve(ata, atb), 1]);
        return ndInv.After(hn.After(ns));
    }

    /// <summary>Composition a.After(b) = a ∘ b, i.e. b is applied first.</summary>
    public Homography After(Homography b)
    {
        var a = h; var c = new double[9];
        for (int r = 0; r < 3; r++)
            for (int k = 0; k < 3; k++)
                c[r * 3 + k] = a[r * 3] * b.h[k] + a[r * 3 + 1] * b.h[3 + k] + a[r * 3 + 2] * b.h[6 + k];
        return new Homography(c.Select(v => v / c[8]).ToArray());
    }

    static (Homography n, Homography inv) Normalizer(IReadOnlyList<SKPoint> pts)
    {
        double mx = pts.Average(p => p.X), my = pts.Average(p => p.Y);
        double sd = Math.Sqrt(pts.Average(p => (p.X - mx) * (p.X - mx) + (p.Y - my) * (p.Y - my)));
        double s = sd > 0 ? Math.Sqrt(2) / sd : 1;
        return (new Homography([s, 0, -s * mx, 0, s, -s * my, 0, 0, 1]), new Homography([1 / s, 0, mx, 0, 1 / s, my, 0, 0, 1]));
    }

    static void Accumulate(double[,] ata, double[] atb, double[] row, double rhs, double w)
    {
        for (int r = 0; r < 8; r++)
        {
            atb[r] += w * row[r] * rhs;
            for (int c = 0; c < 8; c++) ata[r, c] += w * row[r] * row[c];
        }
    }

    public Homography Inverse()
    {
        double a = h[0], b = h[1], c = h[2], d = h[3], e = h[4], f = h[5], g = h[6], k = h[7], i = h[8];
        double[] inv = [e * i - f * k, c * k - b * i, b * f - c * e,
                        f * g - d * i, a * i - c * g, c * d - a * f,
                        d * k - e * g, b * g - a * k, a * e - b * d];
        double s = inv[8];
        return new Homography(inv.Select(v => v / s).ToArray());
    }

    static double[] Solve(double[,] a, double[] b)
    {
        int n = b.Length;
        var m = (double[,])a.Clone();
        var x = (double[])b.Clone();
        for (int col = 0; col < n; col++)
        {
            int piv = col;
            for (int r = col + 1; r < n; r++) if (Math.Abs(m[r, col]) > Math.Abs(m[piv, col])) piv = r;
            if (piv != col)
            {
                for (int c = 0; c < n; c++) (m[col, c], m[piv, c]) = (m[piv, c], m[col, c]);
                (x[col], x[piv]) = (x[piv], x[col]);
            }
            for (int r = 0; r < n; r++)
            {
                if (r == col) continue;
                double f = m[r, col] / m[col, col];
                if (f == 0) continue;
                for (int c = col; c < n; c++) m[r, c] -= f * m[col, c];
                x[r] -= f * x[col];
            }
        }
        for (int i = 0; i < n; i++) x[i] /= m[i, i];
        return x;
    }
}
