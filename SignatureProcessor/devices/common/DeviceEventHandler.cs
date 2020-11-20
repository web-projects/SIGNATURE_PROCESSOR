using Devices.Common.Helpers;

namespace Devices.Common
{
    public delegate void DeviceEventHandler(DeviceEvent deviceEvent, DeviceInformation deviceInformation);
    public delegate void ComPortEventHandler(PortEventType comPortEvent, string portNumber);
    public delegate void QueueEventOccured();
}
