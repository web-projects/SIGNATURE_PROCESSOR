namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// If VAS transaction was performed, the C6 template can be returned within any of the following templates:
    /// E3, E4, E5, E7, E8
    /// This depends on the outcome of Payment transaction.
    /// </summary>
    public static class C6Template
    {
        public static readonly uint C6TemplateTag = 0xC6;
        public static readonly uint VASIdentifier = 0xDFC601;
        public static readonly uint VASData = 0xDFC602;
    }
}
