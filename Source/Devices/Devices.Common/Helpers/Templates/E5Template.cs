namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// Should the transaction be declined, the data required to store the transaction in a payment system
    /// will be returned in this template.
    /// </summary>
    public static class E5Template
    {
        public static readonly uint E5TemplateTag = 0xE5;
        public static readonly uint ApplicationPAN = 0x5A;
        public static readonly uint ApplicationInterchangeProfile = 0x82;
        public static readonly uint DedicatedFilename = 0x84;
        public static readonly uint AuthorizationResponseCode = 0x8A;
        public static readonly uint TerminalVerificationResults = 0x95;
        public static readonly uint TransactionStatusInformation = 0x9B;
        public static readonly uint IssuerActionCodeDefault = 0x9F0D;
        public static readonly uint IssuerActionCodeDenial = 0x9F0E;
        public static readonly uint IssuerActionCodeOnline = 0x9F0F;
        public static readonly uint IssuerApplicationData = 0x9F10;
        public static readonly uint TerminalType = 0x9F35;
        public static readonly uint ApplicationCryptogram = 0x9F26;
        public static readonly uint CryptogramInformationData = 0x9F27;
        public static readonly uint ApplicationTransactionCounter = 0x9F36;
        public static readonly uint UnpredictableNumber = 0x9F37;
        public static readonly uint PinEntryResult = 0xDFA20A;
    }
}
