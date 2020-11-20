namespace Devices.Verifone.VIPA.Templates
{
    /// <summary>
    /// This template may consist of more details(SBI / CIB / CTLSHW versions). Names are stored in tags
    /// DF8106 and versions in tags DF8107.
    /// </summary>
    class EETemplate
    {
        public static byte[] EETemplateTag = new byte[] { 0xEE };                // EE Template tag
        public static byte[] TerminalNameTag = new byte[] { 0xDF, 0x0D };        // Terminal Name tag
        public static byte[] TerminalIdTag = new byte[] { 0x9F, 0x1C };          // Terminal ID tag
        public static byte[] SerialNumberTag = new byte[] { 0x9F, 0x1E };        // Serial Number tag
        public static byte[] TamperStatus = new byte[] { 0xDF, 0x81, 0x01 };     // Tamper Status tag
        public static byte[] ArsStatus = new byte[] { 0xDF, 0x81, 0x02 };        // ARS Status tag
    }
}
