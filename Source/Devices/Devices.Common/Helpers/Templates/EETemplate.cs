namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// This template may consist of more details(SBI / CIB / CTLSHW versions). Names are stored in tags
    /// DF8106 and versions in tags DF8107.
    /// </summary>
    public static class EETemplate
    {
        public static readonly uint EETemplateTag = 0xEE;
        public static readonly uint TerminalName = 0xDF0D;
        public static readonly uint SerialNumber = 0x9F1E;
        public static readonly uint TamperStatus = 0xDF8101;
        public static readonly uint ArsStatus = 0xDF8102;
        public static readonly uint TerminalCountryCode = 0x9F1A;
        public static readonly byte[] CountryCodeUS = new byte[] { 0x08, 0x40 };
        public static readonly uint TerminalId = 0x9F1C;
        public static readonly uint Reboot24HourTag = 0xDFA242;

        /// <summary>
        /// Online approved
        /// </summary>
        public static readonly byte[] AuthResp00 = new byte[] { 0x30, 0x30 };
        /// <summary>
        /// Offline approved
        /// </summary>
        public static readonly byte[] AuthRespY1 = new byte[] { 0x59, 0x31 };
        /// <summary>
        /// Unable to go online, offline approved
        /// </summary>
        public static readonly byte[] AuthRespY3 = new byte[] { 0x59, 0x33 };
        /// <summary>
        /// Offline declined
        /// </summary>
        public static readonly byte[] AuthRespZ1 = new byte[] { 0x5A, 0x31 };
        /// <summary>
        /// Unable to go online, offline decline
        /// </summary>
        public static readonly byte[] AuthRespZ3 = new byte[] { 0x5A, 0x33 };
        /// <summary>
        /// FiServ has a default value of Z3 for all test case
        /// TODO: replace this with proper value from servicer (can be from cdb?)
        /// </summary>
        public static readonly byte[] DefaultFiServAuthResp = AuthRespZ3;
    }
}
