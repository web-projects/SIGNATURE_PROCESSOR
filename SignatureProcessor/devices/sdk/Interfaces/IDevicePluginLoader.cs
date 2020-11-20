using Devices.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.SDK.Interfaces
{
    public interface IDevicePluginLoader
    {
        List<ICardDevice> FindAvailableDevices(string pluginPath);
        List<string> GetAvailableDevicePlugins();
        void LoadDevicePlugin(string deviceName);
    }
}
