using ShootCounter.TargetScan;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace ShootCounter.Views;

// shows the photo of the target with the detected hits, which can be confirmed, changed, removed or added
public partial class ScanPage : ContentPage
{
    private readonly ScanViewModel _viewModel;
    // maps image pixels to canvas pixels, set on every paint
    private float _scale = 1;
    private SKPoint _offset;
    private SKPoint? _touchStart;

    public ScanPage(ScanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        _viewModel.Changed += (s, e) => MainThread.BeginInvokeOnMainThread(Canvas.InvalidateSurface);
    }

    // the scan can only be left with the buttons, so the result always reaches the caller
    protected override bool OnBackButtonPressed()
    {
        _viewModel.CancelCommand.Execute(null);
        return true;
    }

    private void Canvas_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        var photo = _viewModel.Photo;
        if (photo == null)
            return;

        // show the target area only, so the holes are as large as possible
        var source = TargetArea(photo, _viewModel.Result);
        _scale = Math.Min(e.Info.Width / source.Width, e.Info.Height / source.Height);
        _offset = new SKPoint((e.Info.Width - source.Width * _scale) / 2 - source.Left * _scale,
                              (e.Info.Height - source.Height * _scale) / 2 - source.Top * _scale);

        canvas.Save();
        canvas.Translate(_offset);
        canvas.Scale(_scale);
        using (var image = SKImage.FromBitmap(photo))
            canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        canvas.Restore();

        if (_viewModel.Result == null)
            return;

        float density = (float)DeviceDisplay.MainDisplayInfo.Density;
        float radius = Math.Max((float)_viewModel.Result.HoleRadiusPx * _scale * 1.3f, 9 * density);
        using var ring = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f * density, IsAntialias = true };
        using var font = new SKFont(SKTypeface.FromFamilyName(null, SKFontStyle.Bold), 15 * density);
        using var text = new SKPaint { IsAntialias = true };
        using var label = new SKPaint { Color = new SKColor(0, 0, 0, 170), IsAntialias = true };

        foreach (var hit in _viewModel.Hits)
        {
            var p = ToCanvas(hit.Position);
            var color = hit.Uncertain ? SKColors.Orange
                : hit.Borderline ? SKColors.Yellow
                : hit.Manual ? SKColors.Cyan
                : SKColors.Lime;
            ring.Color = color;
            ring.PathEffect = hit.Uncertain ? SKPathEffect.CreateDash([4 * density, 3 * density], 0) : null;
            canvas.DrawCircle(p, radius, ring);

            string caption = hit.Score + (hit.Uncertain ? "?" : hit.Borderline ? "~" : "");
            float width = font.MeasureText(caption);
            var box = SKRect.Create(p.X + radius + 2 * density, p.Y - radius - 16 * density, width + 8 * density, 19 * density);
            canvas.DrawRoundRect(box, 4 * density, 4 * density, label);
            text.Color = color;
            canvas.DrawText(caption, box.Left + 4 * density, box.Bottom - 4.5f * density, font, text);
        }
    }

    // the interesting part of the target, or the whole photo when no target was found
    private static SKRect TargetArea(SKBitmap photo, TargetScanResult? result)
    {
        var all = SKRect.Create(photo.Width, photo.Height);
        if (result == null)
            return all;

        var a = result.TemplateArea;
        SKPoint[] corners = [new(a.Left, a.Top), new(a.Right, a.Top), new(a.Right, a.Bottom), new(a.Left, a.Bottom)];
        var mapped = corners.Select(result.TemplateToImage.Map).ToArray();
        var area = new SKRect(mapped.Min(n => n.X), mapped.Min(n => n.Y), mapped.Max(n => n.X), mapped.Max(n => n.Y));
        return area.IntersectsWith(all) ? SKRect.Intersect(area, all) : all;
    }

    private SKPoint ToCanvas(SKPoint image) => new(image.X * _scale + _offset.X, image.Y * _scale + _offset.Y);

    private SKPoint ToImage(SKPoint canvas) => new((canvas.X - _offset.X) / _scale, (canvas.Y - _offset.Y) / _scale);

    private async void Canvas_Touch(object? sender, SKTouchEventArgs e)
    {
        e.Handled = true;

        if (e.ActionType == SKTouchAction.Pressed)
        {
            _touchStart = e.Location;
            return;
        }

        if (e.ActionType != SKTouchAction.Released || _touchStart == null)
            return;

        float density = (float)DeviceDisplay.MainDisplayInfo.Density;
        bool isTap = SKPoint.Distance(_touchStart.Value, e.Location) < 12 * density;
        _touchStart = null;

        if (!isTap || _viewModel.Result == null || _viewModel.Photo == null || _viewModel.IsBusy)
            return;

        var position = ToImage(e.Location);
        if (position.X < 0 || position.Y < 0 || position.X >= _viewModel.Photo.Width || position.Y >= _viewModel.Photo.Height)
            return;

        var hit = _viewModel.HitAt(position, Math.Max((float)_viewModel.Result.HoleRadiusPx * 2, 18 * density / _scale));
        if (hit == null)
        {
            _viewModel.AddHit(position);
            return;
        }

        await EditHitAsync(hit);
    }

    private async Task EditHitAsync(ScanHit hit)
    {
        const string confirm = "Bestätigen";
        const string remove = "Löschen";

        var scores = _viewModel.Valuations;
        int index = scores.ToList().IndexOf(hit.Score);
        var options = new List<string>();
        if (hit.Uncertain || hit.Borderline)
            options.Add(confirm);
        if (index >= 0 && index < scores.Count - 1)
            options.Add($"Wertung {scores[index + 1]}");
        if (index > 0)
            options.Add($"Wertung {scores[index - 1]}");

        string title = hit.Uncertain ? $"Unsicherer Treffer ({hit.Score})"
            : hit.Borderline ? $"Linienschuss ({hit.Score})"
            : $"Treffer ({hit.Score})";
        string action = await DisplayActionSheet(title, "Abbrechen", remove, [.. options]);

        if (action == confirm)
            _viewModel.ConfirmHit(hit);
        else if (action == remove)
            _viewModel.RemoveHit(hit);
        else if (action?.StartsWith("Wertung ") == true && int.TryParse(action["Wertung ".Length..], out int score))
            _viewModel.SetScore(hit, score);
    }
}
