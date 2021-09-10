namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// This template is returned as an output of contactless transaction only when a NON-EMV card is
    /// tapped. It contains NON-EMV specific tags as described in Start Contactless Transaction[C0, A0] and
    /// Continue Contactless Transaction.
    /// </summary>
    public static class E8Template
    {
        public static readonly uint E8TemplateTag = 0xE8;
    }
}
