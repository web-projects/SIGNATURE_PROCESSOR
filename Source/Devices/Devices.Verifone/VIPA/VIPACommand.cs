using static System.ExtensionMethods;

namespace Devices.Verifone.VIPA
{
    public class VIPACommand
    {
        public VIPACommand(VIPACommandType commandType)
        {
            cla = (byte)((short)commandType >> 8 & 0xFF);
            ins = (byte)((short)commandType & 0xFF);
        }

        public byte nad { get; set; }
        public byte pcb { get; set; }
        public byte cla { get; set; }
        public byte ins { get; set; }
        public byte p1 { get; set; }
        public byte p2 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] data { get; set; }
        public bool includeLE { get; set; }
        public byte le { get; set; }

        public static readonly string ChainedResponseAnswerData = "mapp/signature.html";
    }

    public enum VIPACommandType
    {
        //FileMaintenance
        SelectFile = 0x00A4,
        ReadBinary = 0x00B0,
        UpdateBinary = 0x00D6,
        RenameCopyBinary = 0x00AA,
        DeleteBinary = 0x00AB,
        GetBinaryStatus = 0x00C0,
        StreamUpload = 0x00A5,
        FindFirstFile = 0x00C3,
        FindNextFile = 0x00C4,
        FreeSpace = 0x00D0,
        PutData = 0x00DA,
        GetData = 0x00CA,
        //SecurityRelated
        UpdateKey = 0xC40A,
        SetSecurityConfiguration = 0xC410,
        GetSecurityConfiguration = 0xC411,
        GenerateMAC = 0xC420,
        VerifyMAC = 0xC421,
        EncryptData = 0xC425,
        DecryptData = 0xC426,
        GenerateHMAC = 0xC422,
        OnlinePIN = 0xDED6,
        ABSA9DigitsAuthorization = 0xDD25,
        ExecuteVSSScript = 0xC413,
        //PowerManagement
        //PoweronNotification
        ICCcardslotcontrol = 0xD05F,
        PowerManagement = 0xD063,
        BatteryStatus = 0xD062,
        //Device / SoftwareManagement
        ResetDevice = 0xD000,
        ExtendedSoftwareResetDevice = 0xD00A,
        CardStatus = 0xD060,
        KeyboardStatus = 0xD061,
        ReadVOSCounters = 0xD004,
        DumpLogs = 0xD005,
        UxRemoteSysmode = 0xD006,
        GetSetDateTime = 0xDD10,
        LogConfiguration = 0xD064,
        UpdatePINPADfirmware = 0xD20A,
        BTBaseOSupdate = 0xD2BB,
        //GenericOperations
        Abort = 0xD0FF,
        Disconnect = 0xD003,
        ConfigurationFileVersions = 0xD001,
        //EMVContact – Level 1                                
        VerifyPIN = 0xDED5,
        AtomicVerifyPIN = 0xDED7,
        //Direct Access to a SmartCard
        StartTransaction = 0xDED1,
        ContinueTransaction = 0xDED2,
        //EMVGeneric
        ManualPANEntry = 0xD214,
        GetEMVHashValues = 0xDE01,
        //ContactlessLibrary – Commands
        GetContactlessStatus = 0xC000,
        OpenandInitialiseContactlessReader = 0xC001,
        CloseContactlessReader = 0xC002,
        StartContactlessTransaction = 0xC0A0,
        ContinueContactlessTransaction = 0xC0A1,
        CancelContactlessTransaction = 0xC0C0,
        ContactlessUI = 0xC010,
        //DisplayManagement
        Display = 0xD201,
        DisplayBitmap = 0xD210,
        DisplayText = 0xD202,
        SelectLanguage = 0xD2D0,
        RequestChoice = 0xD203,
        GetNumericData = 0xD204,
        GetAlphanumericData = 0xD2F1,
        //Implementationonterminal
        //ImplementationonPINPad
        PasswordEntry = 0xD2F3,
        DisplayQR = 0xD2C0,
        DisplayHTML = 0xD2E0,
        VirtualKeyboard = 0xD220,
        //Printing
        PrintData = 0xD2A1,
        PrintBitmap = 0xD2A2,
        PrintBarcode = 0xD2A3,
        GetPrinterStatus = 0xD2A4,
        PrintHTML = 0xD2A5,
        //MemoryCardOperations
        I2CRead = 0xD120,
        I2CWrite = 0xD121,
        MemoryCardRead = 0xD111,
        MemoryCardWrite = 0xD112,
        MemoryCardUpdate = 0xD113,
        TerminalSetGetDataTime = 0xDD10,
        Terminal24HourReboot = 0xD024
    }

    public enum UserInteraction
    {
        [StringValue("UserKeyPressed")]
        UserKeyPressed = 0x5000
    }
}
