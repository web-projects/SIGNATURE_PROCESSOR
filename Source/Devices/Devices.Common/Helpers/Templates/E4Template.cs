namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// Template E4 - Online Action Required
    /// Where online action is required to complete the transaction, this template will be returned
    /// containing all the data required to perform this action.
    /// </summary>
    public static class E4Template
    {
        public static readonly uint E4TemplateTag = 0xE4;
        /// <summary>
        /// Tag 50
        /// </summary>
        public static readonly uint ApplicationLabel = 0x50;
        /// <summary>
        /// Tag 5A
        /// </summary>
        public static readonly uint ApplicationPAN = 0x5A;
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
        /// Tag 9F07
        /// </summary>
        public static readonly uint ApplicationUsageControl = 0x9F07;
        /// <summary>
        /// Tag 5F34
        /// </summary>
        public static readonly uint ApplicationPANSequenceNumber = 0x5F34;
        /// <summary>
        /// Tag 5F24
        /// </summary>
        public static readonly uint ExpirationDate = 0x5F24;
        /// <summary>
        /// Tag 57
        /// </summary>
        public static readonly uint Track2EquivalentData = 0x57;
        /// <summary>
        /// <para>Tag DFA20A</para>
        /// <br>0 - PIN entry completed</br> 
        /// <br>1 - PIN entry timed out</br>
        /// <br>2 - PIN entry cancelled</br>
        /// <br>3 - PIN entry comparison OK</br>
        /// <br>4 - PIN entry bypassed</br>
        /// </summary>
        public static readonly uint PinEntryResult = 0xDFA20A;
        /// <summary>
        /// Tag 82
        /// </summary>
        public static readonly uint ApplicationInterchangeProfile = 0x82;
        /// <summary>
        ///<para>Tag 95</para> 
        ///<br>8000000000(Byte 1 Bit 8) Offline data authentication was not performed</br>
        ///<br>4000000000(Byte 1 Bit 7) SDA failed</br>
        ///<br>2000000000(Byte 1 Bit 6) ICC data missing</br>
        ///<br>1000000000(Byte 1 Bit 5) Card appears on terminal exception file</br>
        ///<br>0800000000(Byte 1 Bit 4) DDA failed</br>
        ///<br>0200000000(Byte 1 Bit 2) SDA selected</br>
        ///<br>0400000000(Byte 1 Bit 3) CDA failed</br>
        ///<br>0080000000(Byte 2 Bit 8) ICC and terminal have different application versions</br>
        ///<br>0040000000(Byte 2 Bit 7) Expired application</br>
        ///<br>0020000000(Byte 2 Bit 6) Application not yet effective</br>
        ///<br>0010000000(Byte 2 Bit 5) Requested service not allowed for card product</br>
        ///<br>0008000000(Byte 2 Bit 4) New card</br>
        ///<br>0000800000(Byte 3 Bit 8) Cardholder verification was not successful</br>
        ///<br>0000400000(Byte 3 Bit 7) Unrecognised CVM</br>
        ///<br>0000200000(Byte 3 Bit 6) PIN try limit exceeded</br>
        ///<br>0000100000(Byte 3 Bit 5) PIN entry required and PIN pad not present or not working</br>
        ///<br>0000080000(Byte 3 Bit 4) PIN entry required, PIN pad present, but PIN was not entered</br>
        ///<br>0000040000(Byte 3 Bit 3) Online PIN entered</br>
        ///<br>0000008000(Byte 4 Bit 8) Transaction exceeds floor limit</br>
        ///<br>0000004000(Byte 4 Bit 7) Lower consecutive offline limit exceeded</br>
        ///<br>0000002000(Byte 4 Bit 6) Upper consecutive offline limit exceeded</br>
        ///<br>0000001000(Byte 4 Bit 5) Transaction selected randomly for online processing</br>
        ///<br>0000000800(Byte 4 Bit 4) Merchant forced transaction online</br>
        ///<br>0000000080(Byte 5 Bit 8) Default TDOL used</br>
        ///<br>0000000040(Byte 5 Bit 7) Issuer authentication failed</br>
        ///<br>0000000020(Byte 5 Bit 6) Script processing failed before final GENERATE AC</br>
        ///<br>0000000010(Byte 5 Bit 5) Script processing failed after final GENERATE AC</br>
        /// </summary>
        public static readonly uint TerminalVerificationResults = 0x95;
        /// <summary>
        /// <para>Tag 9B</para>
        /// A record of things that happened during the transaction.
        /// Whilst the TVR is expected to mainly be zeroes.This field is expected to mainly be ones.Each bit is a fact about the transaction.
        /// <br>TYPICAL VALUE: E800</br>
        ///<br>8000(Byte 1 Bit 8) Offline data authentication was performed</br>
        ///<br>4000(Byte 1 Bit 7) Cardholder verification was performed</br>
        ///<br>2000(Byte 1 Bit 6) Card risk management was performed</br>
        ///<br>1000(Byte 1 Bit 5) Issuer authentication was performed</br>
        ///<br>0800(Byte 1 Bit 4) Terminal risk management was performed</br>
        ///<br>0400(Byte 1 Bit 3) Script processing was performed</br>
        ///<br>0080(Byte 2 Bit 8) RFU</br>
        ///<br>0001(Byte 2 Bit 1) RFU</br>
        /// </summary>
        public static readonly uint TransactionStatusInformation = 0x9B;
        /// <summary>
        /// Tag 9C
        /// </summary>
        public static readonly uint TransactionType = 0x9C;
        /// <summary>
        /// Tag 5F2A
        /// </summary>
        public static readonly uint TransactionCurrencyCode = 0x5F2A;
        /// <summary>
        /// Tag 9F02
        /// </summary>
        public static readonly uint AmountAuthorized = 0x9F02;
        /// <summary>
        /// Tag 9F0D
        /// </summary>
        public static readonly uint IssuerActionCodeDefault = 0x9F0D;
        /// <summary>
        /// Tag 9F0E
        /// </summary>
        public static readonly uint IssuerActionCodeDenial = 0x9F0E;
        /// <summary>
        /// Tag 9F0F
        /// </summary>
        public static readonly uint IssuerActionCodeOnline = 0x9F0F;
        /// <summary>
        /// Tag 9F10
        /// </summary>
        public static readonly uint IssuerApplicationData = 0x9F10;
        /// <summary>
        /// Tag 9F33
        /// </summary>
        public static readonly uint TerminalCapabilities = 0x9F33;
        /// <summary>
        /// Tag 9F34
        /// </summary>
        public static readonly uint CardHolderVerificationMethodResults = 0x9F34;
        /// <summary>
        /// Tag 9F35
        /// </summary>
        public static readonly uint TerminalType = 0x9F35;
        /// <summary>
        /// Tag 9F36
        /// </summary>
        public static readonly uint ApplicationTransactionCounter = 0x9F36;
        /// <summary>
        /// Tag 9F37
        /// </summary>
        public static readonly uint UnpredictableNumber = 0x9F37;
        /// <summary>
        /// Tag 5F25
        /// </summary>
        public static readonly uint ApplicationEffectiveData = 0x5F25;
        /// <summary>
        /// Tag 4F
        /// </summary>
        public static readonly uint ApplicationIdentifierICC = 0x4F;
        /// <summary>
        /// Tag 8A
        /// </summary>
        public static readonly uint AuthorizationResponseCode = 0x8A;
        /// <summary>
        /// Tag 9F26
        /// </summary>
        public static readonly uint ApplicationCryptogram = 0x9F26;
        /// <summary>
        /// Tag 9F27
        /// </summary>
        public static readonly uint CryptogramInformationData = 0x9F27;
        /// <summary>
        /// Tag 9F39
        /// </summary>
        public static readonly uint POSEntryMode = 0x9F39;
        /// <summary>
        /// Tag 9F41
        /// </summary>
        public static readonly uint TransactionSequenceNumber = 0x9F41;
        /// <summary>
        /// Tag 9F1E
        /// </summary>
        public static readonly uint InterfaceDeviceSerialNumber = 0x9F1E;
        /// <summary>
        /// Tag 9F40
        /// </summary>
        public static readonly uint AdditionalTerminalCapabilities = 0x9F40;
        /// <summary>
        /// Tag 84
        /// </summary>
        public static readonly uint DedicatedFilename = 0x84;
        /// <summary>
        /// Tag 5F2D
        /// </summary>
        public static readonly uint LanguagePreference = 0x5F2D;
        /// <summary>
        /// Tag9F6E
        /// </summary>
        public static readonly uint ThirdPartyData = 0x9F6E;
        /// <summary>
        /// Tag 9F03
        /// </summary>
        public static readonly uint AmountOther = 0x9F03;
        /// <summary>
        /// Tag 9F53
        /// </summary>
        public static readonly uint TransactionCategoryCode = 0x9F53;
        /// <summary>
        /// Tag DFDF08
        /// </summary>
        public static readonly uint TACOnline = 0xDFDF08;
        /// <summary>
        /// Tag DFDF07
        /// </summary>
        public static readonly uint TACDenial = 0xDFDF07;
        /// <summary>
        /// Tag DFDF06
        /// </summary>
        public static readonly uint TACDefault = 0xDFDF06;
        /// <summary>
        /// Tag 8E
        /// </summary>
        public static readonly uint CVMList = 0x8E;
        /// <summary>
        /// Tag 5F20
        /// </summary>
        public static readonly uint CardholderName = 0x5F20;
        /// <summary>
        /// Tag 5F28
        /// </summary>
        public static readonly uint IssuerCountryCode = 0x5F28;
        /// <summary>
        /// Tag 5F30
        /// </summary>
        public static readonly uint ServiceCode = 0x5F30;
        /// <summary>
        /// Tag 9F4C
        /// </summary>
        public static readonly uint ICCDynamicNumber = 0x9F4C;
    }
}
