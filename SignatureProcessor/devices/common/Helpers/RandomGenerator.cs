using System;

namespace Devices.Common.Helpers
{
    public static class RandomGenerator
    {
        public static Random rnd() => new Random((int)DateTime.Now.Ticks);

        public static int GetRandomValue() => rnd().Next();

        public static int GetRandomValue(int digits) => rnd().Next(1, (int)(Math.Pow(10.0, digits) - 1.0));

        public static string GetRandomValueStr(int digits) => rnd().Next(1, (int)(Math.Pow(10.0, digits) - 1.0)).ToString("G", new System.Globalization.CultureInfo("en-us"));

        public static string BuildRandomString(int string_length)
        {
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var bit_count = (string_length * 6);
                var byte_count = ((bit_count + 7) / 8); // rounded up
                var bytes = new byte[byte_count];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).TrimEnd('=');
            }
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
