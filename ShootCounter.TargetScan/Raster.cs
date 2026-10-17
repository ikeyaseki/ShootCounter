using SkiaSharp;

namespace ShootCounter.TargetScan;

/// <summary>Plain RGB pixel buffer with the low-level operations the scanner needs.</summary>
public sealed class Raster
{
    public readonly int Width, Height;
    public readonly byte[] R, G, B;

    public Raster(int w, int h) { Width = w; Height = h; R = new byte[w * h]; G = new byte[w * h]; B = new byte[w * h]; }

    public static Raster FromBitmap(SKBitmap bmp)
    {
        var r = new Raster(bmp.Width, bmp.Height);
        var px = bmp.Pixels;
        for (int i = 0; i < px.Length; i++) { r.R[i] = px[i].Red; r.G[i] = px[i].Green; r.B[i] = px[i].Blue; }
        return r;
    }

    public static Raster Load(string path, int maxSide)
    {
        using var stream = File.OpenRead(path);
        using var bmp = LoadBitmap(stream, maxSide);
        return FromBitmap(bmp);
    }

    /// <summary>
    /// Decodes a photo upright (EXIF orientation applied) with its longer side scaled down to maxSide.
    /// JPEGs are decoded at a reduced size directly, so a 12 MP photo never needs full-size memory.
    /// </summary>
    public static SKBitmap LoadBitmap(Stream stream, int maxSide)
    {
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Unsupported image format");
        var size = codec.Info.Size;
        float scale = Math.Min(1f, (float)maxSide / Math.Max(size.Width, size.Height));
        // the codec only supports certain scales (1/2, 1/4, 1/8 for JPEG); pick one not smaller than needed
        var decodeSize = codec.GetScaledDimensions(scale);
        if (decodeSize.Width < size.Width * scale) decodeSize = size;
        using var decoded = SKBitmap.Decode(codec, new SKImageInfo(decodeSize.Width, decodeSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul));

        int w = (int)Math.Round(size.Width * scale), h = (int)Math.Round(size.Height * scale);
        bool swap = codec.EncodedOrigin is SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftBottom;
        var result = new SKBitmap(swap ? h : w, swap ? w : h);
        using var canvas = new SKCanvas(result);
        switch (codec.EncodedOrigin)
        {
            case SKEncodedOrigin.RightTop: canvas.Translate(result.Width, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(result.Width, result.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, result.Height); canvas.RotateDegrees(270); break;
        }
        using var image = SKImage.FromBitmap(decoded);
        canvas.DrawImage(image, new SKRect(0, 0, w, h), new SKSamplingOptions(SKCubicResampler.Mitchell));
        return result;
    }

    public SKBitmap ToBitmap()
    {
        var bmp = new SKBitmap(Width, Height);
        var px = new SKColor[Width * Height];
        for (int i = 0; i < px.Length; i++) px[i] = new SKColor(R[i], G[i], B[i]);
        bmp.Pixels = px;
        return bmp;
    }

    public int Gray(int i) => (R[i] + G[i] + B[i]) / 3;

    public (double r, double g, double b) Sample(double x, double y)
    {
        if (x < 0 || y < 0 || x >= Width - 1 || y >= Height - 1) return (0, 0, 0);
        int x0 = (int)x, y0 = (int)y; double fx = x - x0, fy = y - y0;
        int i = y0 * Width + x0;
        double L(byte[] c) => (c[i] * (1 - fx) + c[i + 1] * fx) * (1 - fy) + (c[i + Width] * (1 - fx) + c[i + Width + 1] * fx) * fy;
        return (L(R), L(G), L(B));
    }

    /// <summary>Warp into a new raster: dstToSrc maps destination pixel coords to this raster.</summary>
    public Raster Warp(Homography dstToSrc, int w, int h)
    {
        var o = new Raster(w, h);
        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
            {
                var p = dstToSrc.Map(x, y);
                var (r, g, b) = Sample(p.X, p.Y);
                int i = y * w + x;
                o.R[i] = (byte)r; o.G[i] = (byte)g; o.B[i] = (byte)b;
            }
        });
        return o;
    }
}

public static class Mask
{
    /// <summary>Square-window dilation/erosion with radius r, via an integral image.</summary>
    public static bool[] Dilate(bool[] m, int w, int h, int r) => BoxThreshold(m, w, h, r, (s, n) => s > 0);
    public static bool[] Erode(bool[] m, int w, int h, int r) => BoxThreshold(m, w, h, r, (s, n) => s == n);

    static bool[] BoxThreshold(bool[] m, int w, int h, int r, Func<int, int, bool> pred)
    {
        var ii = new int[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            int row = 0;
            for (int x = 0; x < w; x++)
            {
                row += m[y * w + x] ? 1 : 0;
                ii[(y + 1) * (w + 1) + x + 1] = ii[y * (w + 1) + x + 1] + row;
            }
        }
        var o = new bool[w * h];
        Parallel.For(0, h, y =>
        {
            int y0 = Math.Max(0, y - r), y1 = Math.Min(h, y + r + 1);
            for (int x = 0; x < w; x++)
            {
                int x0 = Math.Max(0, x - r), x1 = Math.Min(w, x + r + 1);
                int s = ii[y1 * (w + 1) + x1] - ii[y0 * (w + 1) + x1] - ii[y1 * (w + 1) + x0] + ii[y0 * (w + 1) + x0];
                o[y * w + x] = pred(s, (x1 - x0) * (y1 - y0));
            }
        });
        return o;
    }

    public sealed record Component(int Label, int Area, double Cx, double Cy, int MinX, int MinY, int MaxX, int MaxY)
    {
        public int BoxW => MaxX - MinX + 1;
        public int BoxH => MaxY - MinY + 1;
    }

    /// <summary>4-connected components; returns label image (0 = background) and component stats.</summary>
    public static (int[] labels, List<Component> comps) Components(bool[] m, int w, int h)
    {
        var labels = new int[w * h];
        var comps = new List<Component>();
        var stack = new Stack<int>();
        int next = 0;
        for (int start = 0; start < m.Length; start++)
        {
            if (!m[start] || labels[start] != 0) continue;
            next++;
            int area = 0; long sx = 0, sy = 0; int minX = w, minY = h, maxX = 0, maxY = 0;
            labels[start] = next; stack.Push(start);
            while (stack.Count > 0)
            {
                int p = stack.Pop(); int x = p % w, y = p / w;
                area++; sx += x; sy += y;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                if (x > 0) Try(p - 1);
                if (x < w - 1) Try(p + 1);
                if (y > 0) Try(p - w);
                if (y < h - 1) Try(p + w);
            }
            comps.Add(new Component(next, area, (double)sx / area, (double)sy / area, minX, minY, maxX, maxY));
        }
        return (labels, comps);

        void Try(int q) { if (m[q] && labels[q] == 0) { labels[q] = next; stack.Push(q); } }
    }
}
