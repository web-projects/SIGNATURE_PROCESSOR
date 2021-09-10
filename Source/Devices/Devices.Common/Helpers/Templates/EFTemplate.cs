namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// This template encapsulates the whitelist hash from the device.
    /// </summary>
    public static class EFTemplate
    {
        public static readonly uint EFTemplateTag = 0xEF;
        public static readonly uint WhiteListHash = 0xDFDB09;
        public static readonly uint FirmwareVersion = 0xDF7F;
    }
}
