using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Ninject;
using System;

namespace Devices.Common
{
    internal class DiscoverDevice
    {
        [Inject]
        internal IDeviceProvider DeviceProvider { get; set; }

        public bool DiscoverDevices(string name)
        {
            ICardDevice device = DeviceProvider.GetDevice(name);

            if (device == null)
            {
                throw new Exception(StringValueAttribute.GetStringValue(DeviceDiscovery.NoDeviceAvailable));
            }

            return device.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
