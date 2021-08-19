using SignatureProcessorApp.devices.common;

namespace Devices.Common
{
    public class DeviceConfig
    {
        public enum DeviceConnectionType
        {
            Unknown,
            Comm,
            USB,
            TCPIP
        }

        public enum DeviceType
        {
            DeviceVerifone,
            DeviceIdTech,
            DeviceSimulator,
            NoDevice
        }

        public DeviceConnectionType ConnectionType { get; set; }
        public string Connection { get; set; }
        public int? Speed { get; set; }
        public int? Timeout { get; set; }
        public bool Valid { get; set; } = false;
        public SerialDeviceConfig SerialConfig { get; private set; }
        public SupportedTransactions SupportedTransactions { get; set; }
        public DeviceConfig SetSerialDeviceConfig(in SerialDeviceConfig serialDeviceConfig)
        {
            SerialConfig = serialDeviceConfig;
            return this;
        }
    }

    public class SerialDeviceConfig
    {
        public string CommPortName { get; set; } = "COM11";
        public int CommBaudRate { get; set; } = 57600;
        public System.IO.Ports.Parity CommParity { get; set; } = System.IO.Ports.Parity.None;
        public int CommDataBits { get; set; } = 8;
        public System.IO.Ports.StopBits CommStopBits { get; set; } = System.IO.Ports.StopBits.One;
        public System.IO.Ports.Handshake CommHandshake { get; set; } = System.IO.Ports.Handshake.RequestToSend;
        public int CommReadTimeout { get; set; } = 500;
        public int CommWriteTimeout { get; set; } = 500;
    }
}
