namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// Template E0 is used to provide the device with data objects, decision results or host responses
    /// from an online action.
    /// </summary>
    public static class E0Template
    {
        public static readonly uint E0TemplateTag = 0xE0;
        public static readonly uint CardStatus = 0x48;
        public static readonly uint ATRResponse = 0x63;
        public static readonly uint Track1 = 0x5F21;
        public static readonly uint Track2 = 0x5F22;
        public static readonly uint Track1BCD = 0x5A;
        public static readonly uint Track2BCD = 0x57;
        public static readonly uint MsrTrackStatus = 0xDFDF6E;
        public static readonly uint KeyPress = 0xDFA205;
        public static readonly uint PinpadCypher = 0xDFED6C;
        public static readonly uint PinpadKSN = 0xDFED03;
        public static readonly uint PinEntryTimeout = 0xDFA20E;
        public static readonly uint PinTryFlag = 0xDFEC05;
        public static readonly uint PinEntryType = 0xDFEC7D;
        public static readonly uint PinMinimumLength = 0xDFED04;
        public static readonly uint PinMaximumLength = 0xDFED05;
        public static readonly uint PaymentAmount = 0xDFDF17;
        public static readonly uint TransactionCurrency = 0xDFDF24;
        public static readonly uint TransactionCurrencyExponent = 0xDFDF1C;
        public static readonly uint QuickChipTransaction = 0xDFCC79;
        public static readonly uint PinFirstDigitTimeout = 0xDFB00E;
        public static readonly uint PinRemainingDigitsTimeout = 0xDF000F;
        public static readonly uint RefundFlow = 0xDFA21F;
        public static readonly uint TransactionType = 0x9C;
        public static readonly uint VIPATransactionType = 0xDFDF1D;    //Almost equavalent to 9Cused for PIN Entry only
        public static readonly uint TransactionDate = 0x9A;
        public static readonly uint TransactionTime = 0x9F21;
        public static readonly uint TransactionAmount = 0x9F02;
        public static readonly uint TransactionAmountOther = 0x9F03;
        public static readonly uint TransactionCurrencyCode = 0x5F2A;
        public static readonly uint CLessResults = 0xDFDF30;
        public static readonly uint TransactionOutcome = 0xDFC036;
        public static readonly uint HostDecision = 0xC0;
        public static readonly uint OnlineApproval = 0xC2;
        
        public static readonly uint AuthorizationCode = 0x89;
        public static readonly uint AuthorizationResponseCode = 0x8A;

        public static readonly uint MenuTitle = 0xDFA211;
        public static readonly uint NumericListStyle = 0xDFA212;
        public static readonly uint PinEntryStyle = 0xDFA218;
        public static readonly uint PinpadInput = 0xDF8301;
        public static readonly uint PinpadList = 0xDFA202;
        public static readonly uint PinpadOption3 = 0xDFA203;
        public static readonly uint PinpadZip = 0xDFA208;
        public static readonly uint PinpadABSA = 0xDFDF30;
        public static readonly uint OnlinePINKSN = 0xDFED03;
        public static readonly uint InitVector = 0xDFDF12;
        public static readonly uint SRedCardKSN = 0xDFDF11;
        public static readonly uint EncryptedKeyCheck = 0xDFDF10;
        public static readonly uint KeySlotNumber = 0xDFEC2E;
        public static readonly uint ApplicationAID = 0x9F06;
        public static readonly uint KernelConfiguration = 0xDFDF05;
        public static readonly uint Cryptogram = 0xDFEC7B;
        public static readonly uint CommandCode = 0xDFA501;
        public static readonly uint MACGenerationData = 0xDFEC0E;
        public static readonly uint MACHostId = 0xDFEC23;
        public static readonly uint EMVKernelAidGenerator = 0x9F060E;
        public static readonly uint LanguageFileIndex = 0xDFA206;
        public static readonly uint NumberFormat = 0xDFA207;
        public static readonly uint LengthOfEntry = 0xDF8305;
        public static readonly uint MinimumLengthOfEntry = 0xDF8306;
        public static readonly uint AllowedPasswordEntryMode = 0xDFB005;
        public static readonly uint SuppressDisplay = 0xDFA214;       //This does not exist in VIPA documentation?
        public static readonly uint ExternalApplicationSelection = 0xDFA204;       //This does not exist in VIPA documentation?
        public static readonly uint OnlinePINBlockKSN = 0xDFED0D;

        // Manual PAN Entry
        public static readonly uint ManualPANData = 0xDFDB01;
        public static readonly uint ManualCVVData = 0xDFDB02;
        public static readonly uint ManualExpiryData = 0xDFDB03;
        public static readonly uint ManualLuhnCheck = 0xDFDF20;
        public static readonly uint ManualPANMaxLength = 0xDF8305;

        // embedded in TAG FF7C
        public static readonly uint ManualPANEncryptionKey = 0xDF836F;
        public static readonly uint ManualPANNumber = 0xDF837F;
        public static readonly uint Manual3DESKCV = 0xDF837E;
        public static readonly uint CardholderName = 0x5F20;

        // Merchant related
        public static readonly uint MerchantIdentifier = 0x9F16;
        public static readonly uint MerchantNameLoc = 0x9F4E;
        public static readonly uint IssuerURL = 0x5F50;

        // Display Text
        public static readonly uint DisplayText = 0xDF8104;

        public static readonly uint ResetDeviceFlags = 0xDFED0D;

        //Expected Tag data values
        public static readonly byte[] AbortedPinPadEntry = new byte[] { 0x41, 0x42, 0x4F, 0x52, 0x54, 0x45, 0x44 };
        public static readonly byte[] AcquirerIdentification = new byte[] { 0x36, 0x35 };
        public static readonly byte[] CardRead = new byte[] { 0x00, 0x01 };
        public static readonly byte[] BadCardRead = new byte[] { 0x00, 0x00 };

        // HTML support
        public static readonly uint HTMLResourceName = 0xDFAA01;
        public static readonly uint HTMLKeyName = 0xDFAA02;
        public static readonly uint HTMLValueName = 0xDFAA03;
        public static readonly uint HTMLKeyPress = 0xDFAA05;
    }
}
