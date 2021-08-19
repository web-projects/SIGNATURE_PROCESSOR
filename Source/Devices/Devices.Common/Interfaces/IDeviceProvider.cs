namespace Devices.Common.Interfaces
{
    internal interface IDeviceProvider
    {
        ICardDevice GetDevice(string deviceName);
    }
}
