using ShootCounter.TargetScan;
static class Hits50
{
    public static void Run(string path)
    {
        var img = Raster.Load(path, 1600);
        var res = Scanner50m.Scan(img, Raster.Load(path, 99999))!;
        var inv = res.TemplateToImage.Inverse();
        foreach (var h in res.Hits) Console.WriteLine($"hit ({h.Image.X},{h.Image.Y}) d={Math.Sqrt(h.Template.X * h.Template.X + h.Template.Y * h.Template.Y):0}mm score={h.Score}");
        foreach (var b in Blobs50m.Find(img, res, 60))
        {
            var t = inv.Map(b.Image); double d = Math.Sqrt(t.X * t.X + t.Y * t.Y);
            if (d < 115) Console.WriteLine($"blob ({b.Image.X},{b.Image.Y}) d={d:0}mm resp={b.Response:0} peak={b.Peak:0} rgb=({b.R:0},{b.G:0},{b.B:0}) edge={b.Edge:0}");
        }
    }
}
