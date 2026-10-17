using SkiaSharp;
using ShootCounter.TargetScan;

/// <summary>Dev tool: sheet of zoomed crops around the target centre to verify the detected hits.</summary>
static class Crops
{
    public static SKBitmap CentreCrop(SKBitmap annotated, ScanResult res, string name)
    {
        // region: template ±170 x ±260 mm around the centre
        var pts = new[] { new SKPoint(-170, -260), new SKPoint(170, -260), new SKPoint(170, 260), new SKPoint(-170, 260) }
            .Select(res.TemplateToImage.Map).ToArray();
        var src = SKRect.Create(pts.Min(p => p.X), pts.Min(p => p.Y), 0, 0);
        src.Right = pts.Max(p => p.X); src.Bottom = pts.Max(p => p.Y);
        var crop = new SKBitmap(500, 760);
        using var c = new SKCanvas(crop);
        c.Clear(SKColors.Gray);
        c.DrawBitmap(annotated, src, new SKRect(0, 0, 500, 760));
        using var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 28);
        using var p = new SKPaint { Color = SKColors.Cyan };
        c.DrawText(name, 6, 30, font, p);
        return crop;
    }
}
