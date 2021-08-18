namespace Devices.Verifone.VIPA.Templates
{
    /// <summary>
    /// This template encapsulates the whitelist hash from the device.
    /// </summary>
    class EFTemplate
    {
        public static byte[] EFTemplateTag = new byte[] { 0xEF };                // EF Template tag
        public static byte[] WhiteListHash = new byte[] { 0xDF, 0xDB, 0x09 };    // Whitelist tag
        public static byte[] FirmwareVersion = new byte[] { 0xDF, 0x7F };
    }
}
