using System;

namespace Devices.Common.SerialPort
{
    public interface ISerialPortMonitor : IDisposable
    {
        event ComPortEventHandler ComportEventOccured;
        void StartMonitoring();
        void StopMonitoring();
    }
}
