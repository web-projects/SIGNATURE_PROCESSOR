namespace Devices.Common.Helpers.Templates
{
    /// <summary>
    /// Should the transaction enter a wait state, such as PIN entry, this message will indicate to the
    /// integrator why the device is waiting.This data object list is not configurable.
    /// </summary>
    public static class E6Template
    {
        /// <summary>    
        /// Tag E6       
        /// </summary>
        public static readonly uint E6TemplateTag =  0xE6;
        /// <summary>
        /// <para>Tag C3</para>
        /// <br>Value 0x02: PIN entry handoff</br>
        /// <br>Value 0x0C: PIN entry is requested</br>
        /// <br>Value 0x0E: PIN bypass confirmation</br>
        /// </summary>
        public static readonly uint TransactionStatus =  0xC3;
        /// <summary>
        /// Tag C4
        /// </summary>
        public static readonly uint TransactionStatusMessage =  0xC4;
        /// <summary>
        /// Tag C5
        /// </summary>
        public static readonly uint TransactionPinEntryCount =  0xC5;
        /// <summary>
        /// Tag DFED0A
        /// </summary>
        public static readonly uint TransactionPinAlgorithm =  0xDFED0A;
        /// <summary>
        /// Tag DFCC45
        /// </summary>
        public static readonly uint TransactionPinEntryStatus =  0xDFCC45;
    }
}
