using static System.ExtensionMethods;

namespace Devices.Verifone.VIPA
{
    public enum VipaSW1SW2Codes
    {
        [StringValue("Command success")]
        Success = 0x9000,
        [StringValue("File not found or not accessible")]
        FileNotFoundOrNotAccessible = 0x9f13,
        [StringValue("PIN Bypassed")]
        PinBypass = 0x9015,
        [StringValue("Invalid data")]
        InvalidData = 0x9f21,
        [StringValue("Card Removed")]
        CardRemoved = 0x9f23,
        [StringValue("Bad Card")]
        BadCard = 0x9f25,
        [StringValue("Data Missing")]
        DataMissing = 0x9f27,
        [StringValue("Unsupported card")]
        UnsupportedCard = 0x9f28,
        [StringValue("Contactless Collision Detected")]
        ContactlessCollision = 0x9f31,
        [StringValue("Contactless transaction failed")]
        CLessTransactionFail = 0x9f33,                  //This is "Use another interface on VIPA
        [StringValue("Use Chip - swipe not allowed")]
        UseChip = 0x9f34,
        [StringValue("Consumer CVM")]
        ConsumerCVM_CLess = 0x9f35,
        [StringValue("Contactless card in field")]
        CLessCardInField = 0x9f36,
        [StringValue("Retap card")]
        RetapCard = 0x9f37,
        [StringValue("Command cancelled")]
        CommandCancelled = 0x9f41,
        [StringValue("Cashback not supported")]
        CashbackFail = 0x9f42,
        [StringValue("User entry cancelled")]
        UserEntryCancelled = 0x9f43,
        [StringValue("Correction key pressed")]
        UserEntryCorrected = 0x9f45,
        [StringValue("Failure")]
        Failure = 0xFFFF
    }
}
