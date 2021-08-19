using System.Collections.Generic;

namespace DEVICE_SDK.Sdk
{
    public class DeviceConfigData
    {
        public string Manufacturer { get; set; }
        public string DynamicLibraryName { get; set; }
        public List<DeviceConnectInfo> ConnectInfos { get; } = new List<DeviceConnectInfo>();
    }
}
