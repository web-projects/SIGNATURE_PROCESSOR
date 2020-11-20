using System;
using System.Collections.Generic;
using System.Text;

namespace Devices.Verifone.VIPA.Templates
{
    /// <summary>
    /// This template is used to validate files on the device
    /// </summary>
    class _6FTemplate
    {
        public static byte[] _6fTemplateTag = new byte[] { 0x6F };       // 6F Template tag
        public static byte[] FileSizeTag = new byte[] { 0x80 };
        public static byte[] FileCheckSumTag = new byte[] { 0x88 };
        public static byte[] SecurityStatusTag = new byte[] { 0x89 };
    }
}
