using DEVICE_SDK.Sdk;
using Ninject.Modules;

namespace Devices.SDK.Modules
{
    public class DeviceSdkModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDevicePluginLoader>().To<DevicePluginLoader>();
        }
    }
}
