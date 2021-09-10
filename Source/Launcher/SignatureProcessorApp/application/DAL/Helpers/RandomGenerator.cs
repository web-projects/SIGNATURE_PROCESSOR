using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SignatureProcessorApp.application.DAL.Helpers
{
    public static class RandomGenerator
    {
        public static Random rnd() => new Random((int)DateTime.Now.Ticks);

        public static int GetRandomValue() => rnd().Next();

        public static int GetRandomValue(int digits) => rnd().Next(1, (int)(Math.Pow(10.0, digits) - 1.0));

        public static string GetRandomValueStr(int digits) => rnd().Next(1, (int)(Math.Pow(10.0, digits) - 1.0)).ToString("G", new CultureInfo("en-us"));

        public static string BuildRandomString(int string_length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var bit_count = (string_length * 6);
                var byte_count = ((bit_count + 7) / 8); // rounded up
                var bytes = new byte[byte_count];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).TrimEnd('=');
            }
        }

        public static string BuildRandomHexString(int length)
        {
            const string valid = "0123456789ABCDEF";
            StringBuilder res = new StringBuilder();
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];
                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }
            return res.ToString();
        }

        public static string BuildRandomIntegerString(int length)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < length; i++)
            {
                sb.Append(rnd().Next(10));
            }

            return sb.ToString();
        }
    }
}
