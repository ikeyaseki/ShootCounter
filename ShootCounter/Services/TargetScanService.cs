using System.Diagnostics;

namespace ShootCounter.Services
{
    // takes a photo of the 25m rapid fire target and lets the shooter check the detected hits
    public class TargetScanService
    {
        private const string TakePhoto = "Foto aufnehmen";
        private const string PickPhoto = "Foto aus Galerie";

        // only the ordnance rapid fire target can be recognized so far
        public static bool IsSupported(TargetType targetType) => targetType == TargetType.OrderlyRapidFire;

        // the scores of the accepted hits, highest first, or null if the shooter cancelled
        public async Task<IReadOnlyList<int>?> ScanAsync(int expectedShots, bool allowMoreShots)
        {
            var photo = await GetPhotoAsync();
            if (photo == null)
                return null;

            // the decoder needs a seekable stream, the picker streams are not on every platform
            var buffer = new MemoryStream();
            await using (var stream = await photo.OpenReadAsync())
                await stream.CopyToAsync(buffer);
            buffer.Position = 0;

            var viewModel = new ScanViewModel(expectedShots, allowMoreShots);
            var page = new ScanPage(viewModel);
            await Shell.Current.Navigation.PushModalAsync(page);

            _ = viewModel.AnalyzeAsync(buffer).ContinueWith(_ => buffer.Dispose());
            var result = await viewModel.Completion;

            await Shell.Current.Navigation.PopModalAsync();
            return result;
        }

        private static async Task<FileResult?> GetPhotoAsync()
        {
            var options = MediaPicker.Default.IsCaptureSupported ? new[] { TakePhoto, PickPhoto } : new[] { PickPhoto };
            string source = await Shell.Current.DisplayActionSheet(
                "Ganze Scheibe gerade von vorne fotografieren", "Abbrechen", null, options);

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
