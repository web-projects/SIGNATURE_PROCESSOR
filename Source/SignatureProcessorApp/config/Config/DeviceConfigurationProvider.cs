using Microsoft.Extensions.Configuration;
using System;
using static Config.DeviceConfigConstants;

namespace Config
{
    public class DeviceConfigurationProvider : IDeviceConfigurationProvider
    {
        private static DeviceSection deviceConfig;
        private static IConfiguration rootConfiguration;

        public DeviceSection GetAppConfig()
        {
            return GetAppConfig(rootConfiguration ??
                new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("appsettings.json", optional: true)
               .Build()
            );
        }

        public DeviceSection GetAppConfig(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration), "A configuration object must be present for DAL configuration.");
            }

            rootConfiguration = configuration;

            if (deviceConfig != null)
            {
                return deviceConfig;
            }

            deviceConfig = configuration.GetSection(DeviceSectionKey).Get<DeviceSection>();

            if (deviceConfig == null)
            {
                throw new Exception("Unable to find a devices configuration section.");
            }

            return deviceConfig;
        }

        public IConfiguration GetConfiguration()
        {
            return rootConfiguration;
        }

        public void InitializeConfiguration()
        {
            rootConfiguration = new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("appsettings.json", optional: true)
               .Build();
        }
    }
}
