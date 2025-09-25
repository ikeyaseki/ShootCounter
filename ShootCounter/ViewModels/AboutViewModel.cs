using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootCounter.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        public string Platform { get; } = DeviceInfo.Platform.ToString();
        public string Version { get; } = DeviceInfo.Version.ToString();
        public string DeviceName { get; } = DeviceInfo.Name;
        public string DeviceType { get; } = DeviceInfo.DeviceType.ToString();

        public AboutViewModel()
        {
            Title = "About";
        }
    }
}
