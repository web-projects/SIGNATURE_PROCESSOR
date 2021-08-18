using System;

namespace Config
{
    public class DeviceAppConfig : IAppConfig
    {
        public DeviceProviderType DeviceProvider { get; private set; }

        public IAppConfig SetDeviceProvider(string deviceProviderType)
        {
            if (string.IsNullOrWhiteSpace(deviceProviderType))
            {
                DeviceProvider = DeviceProviderType.Mock;
            }
            else
            {
                DeviceProvider = (DeviceProviderType)Enum.Parse(typeof(DeviceProviderType), deviceProviderType, true);
            }

            return this;
        }
    }
}
