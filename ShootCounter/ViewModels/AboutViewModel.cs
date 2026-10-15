using System.Diagnostics;
using System.Windows.Input;

namespace ShootCounter.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private readonly IDatabaseService _db;

        public string Platform { get; } = DeviceInfo.Platform.ToString();
        public string Version { get; } = DeviceInfo.Version.ToString();
        public string DeviceName { get; } = DeviceInfo.Name;
        public string DeviceType { get; } = DeviceInfo.DeviceType.ToString();
        public string AppVersion { get; } = AppInfo.Version.ToString();
        public string AppBuild { get; } = AppInfo.BuildString;
        public string AppName { get; } = AppInfo.Name;

        public ICommand OpenLinkCommand => new Command<string>(async (url) =>
        {
            if (!string.IsNullOrEmpty(url))
                await Launcher.Default.OpenAsync(url);
        });

        public Command ExportCommand { get; }
        public Command ImportCommand { get; }

        public AboutViewModel(IDatabaseService db)
        {
            _db = db;
            Title = "About";
            ExportCommand = new Command(OnExport, () => !IsBusy);
            ImportCommand = new Command(OnImport, () => !IsBusy);
        }

        // the backup is shared, so it can be saved to the files app, a cloud drive or sent by mail
        private async void OnExport()
        {
            if (!StartWork())
                return;

            try
            {
                // older exports are no longer needed once they were shared
                foreach (var old in Directory.GetFiles(FileSystem.CacheDirectory, "ShootCounter-Sicherung-*.db3"))
                    File.Delete(old);

                var file = Path.Combine(FileSystem.CacheDirectory, $"ShootCounter-Sicherung-{DateTime.Now:yyyy-MM-dd-HHmm}.db3");
                await _db.ExportAsync(file);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "ShootCounter Sicherung",
                    File = new ShareFile(file, "application/octet-stream"),
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Die Daten konnten nicht exportiert werden.", "OK");
            }
            finally
            {
                EndWork();
            }
        }

        private async void OnImport()
        {
            if (!StartWork())
                return;

            try
            {
                var picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Sicherung wählen" });
                if (picked == null)
                    return;

                // the picked file may only be readable as a stream, so it is copied first
                var file = Path.Combine(FileSystem.CacheDirectory, "import.db3");
                await using (var source = await picked.OpenReadAsync())
                await using (var target = File.Create(file))
                {
                    await source.CopyToAsync(target);
                }

                if (!_db.IsValidBackup(file))
                {
                    await Shell.Current.DisplayAlert("Import nicht möglich", "Die gewählte Datei ist keine ShootCounter-Sicherung.", "OK");
                    return;
                }

                bool confirm = await Shell.Current.DisplayAlert(
                    "Daten importieren",
                    "Alle aktuellen Trainings, Wettkämpfe und Waffen werden durch die Sicherung ersetzt. Eine Kopie der aktuellen Daten wird vorher in der App abgelegt.",
                    "Importieren",
                    "Abbrechen");

                if (!confirm)
                    return;

                await _db.ImportAsync(file);
                File.Delete(file);

                await Shell.Current.DisplayAlert("Import abgeschlossen", "Die Daten aus der Sicherung wurden übernommen.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Fehler", "Die Daten konnten nicht importiert werden.", "OK");
            }
            finally
            {
                EndWork();
            }
        }

        // only one export or import at a time
        private bool StartWork()
        {
            if (IsBusy)
                return false;

            IsBusy = true;
            ExportCommand.ChangeCanExecute();
            ImportCommand.ChangeCanExecute();
            return true;
        }

        private void EndWork()
        {
            IsBusy = false;
            ExportCommand.ChangeCanExecute();
            ImportCommand.ChangeCanExecute();
        }
    }
}
