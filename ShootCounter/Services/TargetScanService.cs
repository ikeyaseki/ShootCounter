using ShootCounter.TargetScan;
using System.Diagnostics;

namespace ShootCounter.Services
{
    // takes a photo of the target and lets the shooter check the detected hits
    public class TargetScanService
    {
        private const string TakePhoto = "Foto aufnehmen";
        private const string PickPhoto = "Foto aus Galerie";

        // the targets that can be recognized so far
        private static ScanTarget? ToScanTarget(TargetType targetType) => targetType switch
        {
            TargetType.OrderlyRapidFire => ScanTarget.Ordnance25m,
            TargetType.PrecisionTargetP10 => ScanTarget.PrecisionP10,
            TargetType.PrecisionTargetP4 => ScanTarget.PrecisionP4,
            TargetType.CombinationTargetB5 => ScanTarget.CombinationB5,
            TargetType.CombinationTargetB10 => ScanTarget.CombinationB10,
            TargetType.Morgarten => ScanTarget.Morgarten,
            TargetType.RapidFire => ScanTarget.RapidFire,
            TargetType.PrecisionTargetPP10 => ScanTarget.PrecisionPP10,
            _ => null,
        };

        public static bool IsSupported(TargetType targetType) => ToScanTarget(targetType) != null;

        // the scores of the accepted hits, highest first, or null if the shooter cancelled
        public async Task<IReadOnlyList<int>?> ScanAsync(TargetProperties targetProperties, int expectedShots, bool allowMoreShots)
        {
            var target = ToScanTarget(targetProperties.TargetType)
                ?? throw new NotSupportedException($"Target {targetProperties.TargetType} can not be scanned");

            var photo = await GetPhotoAsync(target);
            if (photo == null)
                return null;

            // the decoder needs a seekable stream, the picker streams are not on every platform
            var buffer = new MemoryStream();
            await using (var stream = await photo.OpenReadAsync())
                await stream.CopyToAsync(buffer);
            buffer.Position = 0;

            var viewModel = new ScanViewModel(target, targetProperties.Valuations ?? [], expectedShots, allowMoreShots);
            var page = new ScanPage(viewModel);
            await Shell.Current.Navigation.PushModalAsync(page);

            _ = viewModel.AnalyzeAsync(buffer).ContinueWith(_ => buffer.Dispose());
            var result = await viewModel.Completion;

            await Shell.Current.Navigation.PopModalAsync();
            return result;
        }

        private static async Task<FileResult?> GetPhotoAsync(ScanTarget target)
        {
            var options = MediaPicker.Default.IsCaptureSupported ? new[] { TakePhoto, PickPhoto } : new[] { PickPhoto };
            string title = target switch
            {
                ScanTarget.Ordnance25m or ScanTarget.RapidFire => "Ganze Scheibe gerade von vorne fotografieren",
                ScanTarget.Morgarten => "Ganze Figur gerade von vorne fotografieren",
                _ => "Spiegel und Ringe bis zum 5er gerade von vorne fotografieren",
            };
            string source = await Shell.Current.DisplayActionSheet(title, "Abbrechen", null, options);

            try
            {
                if (source == TakePhoto)
                {
                    if (await Permissions.RequestAsync<Permissions.Camera>() != PermissionStatus.Granted)
                    {
                        await Shell.Current.DisplayAlert("Kamera", "Ohne Zugriff auf die Kamera kann die Scheibe nicht fotografiert werden.", "OK");
                        return null;
                    }
                    return await MediaPicker.Default.CapturePhotoAsync();
                }

                if (source == PickPhoto)
                    return await MediaPicker.Default.PickPhotoAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Das Foto konnte nicht geladen werden.", "OK");
            }

            return null;
        }
    }
}
