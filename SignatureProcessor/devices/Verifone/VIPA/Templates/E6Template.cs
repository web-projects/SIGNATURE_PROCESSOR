using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Verifone.VIPA.Templates
{
    /// <summary>
    /// Should the transaction enter a wait state, such as PIN entry, this message will indicate to the
    /// integrator why the device is waiting.This data object list is not configurable.
    /// </summary>
    public static class E6Template
    {
        public static byte[] E6TemplateTag = new byte[] { 0xE6 };
        public static byte[] TransactionStatusTag = new byte[] { 0xC3 };
        public static byte[] TransactionStatusMessageTag = new byte[] { 0xC4 };
        public static byte[] TransactionPinEntryCountTag = new byte[] { 0xC5 };
    }
}
