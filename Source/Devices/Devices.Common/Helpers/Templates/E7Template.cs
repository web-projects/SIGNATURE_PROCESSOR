namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// This template is only returned as an output of contactless transactions, when a contactless MSD
    /// card is tapped.
    /// </summary>
    public static class E7Template
    {
        public static readonly uint E7TemplateTag = 0xE7;
        public static readonly uint POSEntryMode = 0x9F39;
    }
}
