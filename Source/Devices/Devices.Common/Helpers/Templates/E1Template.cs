namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// If the device returns this template then it will contain one or more BER‑TLV tags that the device
    /// needs in order to progress the transaction.
    /// </summary>
    public static class E1Template
    {
        public static readonly uint E1TemplateTag = 0xE1;
        public static readonly uint DeviceName = 0xDFC020;
        public static readonly uint SerialNumber = 0xDFC021;
        public static readonly uint FirmwareRevision = 0xDFC022;
        public static readonly uint InitializationStatus = 0xDFC023;
    }
}
