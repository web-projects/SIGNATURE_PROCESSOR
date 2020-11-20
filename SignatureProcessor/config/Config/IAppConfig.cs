namespace Config
{
    public interface IAppConfig
    {
        DeviceProviderType DeviceProvider { get; }
        IAppConfig SetDeviceProvider(string deviceProviderType);
    }
}
