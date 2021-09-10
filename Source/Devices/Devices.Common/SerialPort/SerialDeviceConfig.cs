namespace Devices.Common.SerialPort
{
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
