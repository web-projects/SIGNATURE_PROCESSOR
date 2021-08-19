using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TestHelper
{
    public static class RandomGenerator
    {
        public static Random Random { get; } = new Random();

        public static int GetRandomValue() => Random.Next();

        public static int GetRandomValue(int digits) => Random.Next(1, (int)(Math.Pow(10.0, digits) - 1.0));

        public static string GetRandomValueStr(int digits)
        {
            StringBuilder sb = new StringBuilder();
            for (int ii = 0; ii < digits; ii++)
                sb.Append(Random.Next(0, 9).ToString("G", new CultureInfo("en-us")));
            return sb.ToString();
        }
        public static string BuildRandomString(int string_length)
        {
            using RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            int bit_count = string_length * 6;
            int byte_count = (bit_count + 7) / 8; // rounded up
            byte[] bytes = new byte[byte_count];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).TrimEnd('=');
        }

        public static string BuildRandomIntegerString(int length)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                sb.Append(Random.Next(10));
            }

            return sb.ToString();
        }

        public static byte[] BuildRandomBytes(int length)
        {
            byte[] result = new byte[length];
            Random.NextBytes(result);
            return result;
        }

        public static string BuildRandomHexString(int string_length)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < string_length; i++)
            {
                sb.Append($"{Random.Next(16):X}");
            }

            return sb.ToString();
        }
    }
}
