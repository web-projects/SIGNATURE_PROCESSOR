namespace Devices.Verifone.VIPA.Templates
{
    /// <summary>
    /// Template E0 is used to provide the device with data objects, decision results or host responses
    /// from an online action.
    /// </summary>
    public static class E0Template
    {
        public static byte[] E0TemplateTag = new byte[] { 0xE0 };
        public static byte[] PtidTag = new byte[] { 0x9F, 0x1E };
        public static byte[] CardStatusTag = new byte[] { 0x48 };
        public static byte[] Track1Tag = new byte[] { 0x5F, 0x21 };
        public static byte[] Track2Tag = new byte[] { 0x5F, 0x22 };
        public static byte[] MsrTrackStatus = new byte[] { 0xDF, 0xDF, 0x6E };
        public static byte[] KeyPressTag = new byte[] { 0xDF, 0xA2, 0x05 };
        public static byte[] PinpadCypherTag = new byte[] { 0xDF, 0xED, 0x6C };
        public static byte[] PinpadKSNTag = new byte[] { 0xDF, 0xED, 0x03 };
        public static byte[] PinEntryTimeoutTag = new byte[] { 0xDF, 0xA2, 0x0E };
        public static byte[] QuickChipTransactionTag = new byte[] { 0xDF, 0xCC, 0x79 };
        public static byte[] PinFirstDigitTimeoutTag = new byte[] { 0xDF, 0xB0, 0x0E };
        public static byte[] PinRemainingDigitsTimeoutTag = new byte[] { 0xDF, 0x00, 0x0F };
        public static byte[] RefundFlowTag = new byte[] { 0xDF, 0xA2, 0x1F };
        public static byte[] TransactionType = new byte[] { 0x9C };
        public static byte[] TransactionDate = new byte[] { 0x9A };
        public static byte[] TransactionTime = new byte[] { 0x9F, 0x21 };
        public static byte[] TransactionAmount = new byte[] { 0x9F, 0x02 };
        public static byte[] TransactionCurrencyCode = new byte[] { 0x5F, 0x2A };
        public static byte[] OnlinePINKSNTag = new byte[] { 0xDF, 0xED, 0x03 };
        public static byte[] EncryptedKeyCheckTag = new byte[] { 0xDF, 0xDF, 0x10 };
        public static byte[] SRedCardKSNTag = new byte[] { 0xDF, 0xDF, 0x11 };
        public static byte[] InitVectorTag = new byte[] { 0xDF, 0xDF, 0x12 };
        public static byte[] KeySlotNumberTag = new byte[] { 0xDF, 0xEC, 0x46 };
        public static byte[] ApplicationAIDTag = new byte[] { 0x9F, 0x06 };
        public static byte[] KernelConfigurationTag = new byte[] { 0xDF, 0xDF, 0x05 };
    }
}
