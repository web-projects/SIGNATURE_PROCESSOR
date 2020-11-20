using Microsoft.Extensions.Configuration;

namespace Config
{
    public interface IDeviceConfigurationProvider
    {
        void InitializeConfiguration();
        IConfiguration GetConfiguration();
        DeviceSection GetAppConfig();
        DeviceSection GetAppConfig(IConfiguration configuration);
    }
}
