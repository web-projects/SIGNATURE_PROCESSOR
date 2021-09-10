namespace Devices.Common.Helpers.Templates
{
    public static class SREDTemplate
    {
        public static readonly uint SREDTemplateTag = 0xFF7F;
        public static readonly uint TokenizationTag = 0xFF7C;
        public static readonly uint SREDInputVector = 0xDFDF12;
        public static readonly uint SREDKSN = 0xDFDF11;
        public static readonly uint SREDEncryptedData = 0xDFDF10;
        public static readonly uint SREDEncryptionStatus = 0xDFDB0F;
        public static readonly uint Track1MaskedSRED = 0xDFDB05;
        public static readonly uint Track2MaskedSRED = 0xDFDB06;
        public static readonly uint PanNumberData = 0xDF837F;
    }
}
