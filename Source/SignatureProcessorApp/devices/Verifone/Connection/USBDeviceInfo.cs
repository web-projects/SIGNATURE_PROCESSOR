namespace Devices.Verifone.Connection
{
    public class USBDeviceInfo
    {
        public USBDeviceInfo(string deviceID, string pnpDeviceID, string description, string caption)
        {
            this.DeviceID = deviceID;
            this.PnpDeviceID = pnpDeviceID;
            this.Description = description;
            this.Caption = caption;
        }
        public string DeviceID { get; private set; }
        public string PnpDeviceID { get; private set; }
        public string Description { get; private set; }
        public string Caption { get; private set; }

        public string Identifier { get; set; }
        public string ProductID { get; set; }
        public string SerialNumber { get; set; }
        public string ComPort { get; set; }
    }
    public struct BoolStringDuple
    {
        public bool Item1 { get; set; }
        public string Item2 { get; set; }
        public BoolStringDuple(bool item1, string item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }
}
