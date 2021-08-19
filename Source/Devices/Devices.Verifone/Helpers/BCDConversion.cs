using System;
using System.Buffers;

namespace Devices.Verifone.Helpers
{
    public static class BCDConversion
    {
        private static readonly byte nineKey = 0x27;
        private static readonly byte zeroKey = 0x1e;

        public static byte IntToBCDByte(int intValue)
        {
            byte bcdValue = (byte)((intValue / 10) * 16);
            bcdValue += (byte)(intValue % 10);

            return bcdValue;
        }

        public static byte[] IntToBCD(int numericValue, int byteSize = 6)
        {
            byte[] bcd = ArrayPool<byte>.Shared.Rent(byteSize);

            Array.Clear(bcd, 0, bcd.Length);

            for (int index = 0; index < (byteSize << 1); index++)
            {
                uint hexpart = (uint)(numericValue % 10);
                bcd[index / 2] |= (byte)(hexpart << ((index % 2) * 4));
                numericValue /= 10;
            }

            return bcd;
        }

        public static int BCDToInt(byte[] bcd)
        {
            int result = 0;

            foreach (byte index in bcd)
            {
                result *= 0x100;
                result += (0x10 * (index >> 4) + (index & 0x0F));
            }

            return result;
        }

        public static string StringFromByteData(byte[] byteData)
        {
            if ((byteData?.Length ?? 0) == 0)
                return string.Empty;

            string[] byteStrings = new string[byteData.Length];

            for (int i = 0; i < byteData.Length; i++)
            {
                byteStrings[i] = KeyStringFromByte(byteData[i]);
            }

            return string.Join(",", byteStrings);
        }

        public static string KeyStringFromByte(byte byteValue)
        {
            if (zeroKey <= byteValue && byteValue <= nineKey)
                return $"KEY_{(int)(byteValue - zeroKey)}";
            return byteValue switch
            {
                (byte)138 => "KEY_STAR",
                (byte)139 => "KEY_HASH",
                (byte)27 => "KEY_RED",
                (byte)8 => "KEY_YELLOW",
                (byte)13 => "KEY_GREEN",
                _ => "KEY_Unknown"
            };
        }
    }
}
