using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShootCounter.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
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

        public AboutViewModel()
        {
            Title = "About";
        }
    }
}
