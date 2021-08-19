using Devices.Common.Interfaces;
using System.Collections.Generic;

namespace DEVICE_SDK.Sdk
{
    public interface IDevicePluginLoader
    {
        List<ICardDevice> FindAvailableDevices(string pluginPath);
        List<string> GetAvailableDevicePlugins();
        void LoadDevicePlugin(string deviceName);
    }
}
