namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// Template E2 - Decision Required
    /// Should the device require a decision to be made it will return this template.The template could
    /// contain one or more copies of the same data object with different value fields.
    /// </summary>
    public static class E2Template
    {
        public static readonly uint E2TemplateTag = 0xE2;
        /// <summary>
        /// Tag 50
        /// </summary>
        public static readonly uint ApplicationLabel = 0x50;
        /// <summary>
        /// Tag 5A
        /// </summary>
        public static readonly uint ApplicationPAN = 0x5A;
        /// <summary>
        /// Tag 87
        /// </summary>
        public static readonly uint ApplicationPriority = 0x87;
        /// <summary>
        /// Tag 9F07
        /// </summary>
        public static readonly uint ApplicationUsageControl = 0x9F07;
        /// <summary>
        /// Tag 57
        /// </summary>
        public static readonly uint Track2EquivalentData = 0x57;
        /// <summary>
        /// Tag 5F24
        /// </summary>
        public static readonly uint ExpirationDate = 0x5F24;
        /// <summary>
        /// Tag 5F25
        /// </summary>
        public static readonly uint EffectiveData = 0x5F25;
        /// <summary>
        /// Tag 5F28
        /// </summary>
        public static readonly uint IssuerCountryCode = 0x5F28;
        /// <summary>
        /// Tag 5F30
        /// </summary>
        public static readonly uint ServiceCode = 0x5F30;
        /// <summary>
        /// Tag 5F34
        /// </summary>
        public static readonly uint ApplicationPANSequenceNumber = 0x5F34;
        /// <summary>
        /// Tag 9F06
        /// </summary>
        public static readonly uint ApplicationIdentifier = 0x9F06;
        /// <summary>
        /// Tag 9F08
        /// </summary>
        public static readonly uint AppICCVersion = 0x9F08;
        /// <summary>
        /// Tag 9F09
        /// </summary>
        public static readonly uint AppVersionTerminal = 0x9F09;
        /// <summary>
        /// Tag 9F12
        /// </summary>
        public static readonly uint AppPreferredName = 0x9F12;
        /// <summary>
        /// Tag 9F11
        /// </summary>
        public static readonly uint IssuerCodeTableIndex = 0x9F11;
        /// <summary>
        /// Tag 9C
        /// </summary>
        public static readonly uint TransactionType = 0x9C;
    }
}
