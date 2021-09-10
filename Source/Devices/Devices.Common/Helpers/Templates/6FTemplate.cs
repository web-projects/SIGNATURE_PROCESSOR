namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// This template processes file size, checksum and security status
    /// </summary>
    public static class _6FTemplate
    {
        public static readonly uint _6fTemplateTag = 0x6F;
        public static readonly uint FileNameTag = 0x84;
        public static readonly uint FileSizeTag = 0x80;
        public static readonly uint FileCheckSumTag = 0x88;
        public static readonly uint SecurityStatusTag = 0x89;
        public static readonly uint FCIProprietaryTag = 0xA5;
        public static readonly uint FCIIssuerDiscretionaryTag = 0xBF0C;
        public static readonly uint DirectoryEntryTag = 0x61;
        public static readonly uint ADFNameTag = 0x4F;
    }
}
