using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Common.Config
{
    [Serializable]
    public class NoDeviceSettings
    {
        public int SortOrder { get; set; } = -1;
        public List<string> SupportedDevices { get; internal set; } = new List<string>();
        public bool AllowSimulator { get; set; }
    }
}
