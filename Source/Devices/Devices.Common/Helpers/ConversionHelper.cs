﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Devices.Common.Helpers
{
    public static class ConversionHelper
    {
        /// <summary>
        /// Expects string in Hexadecimal format
        /// </summary>
        /// <param name="valueInHexadecimalFormat"></param>
        /// <returns>returns byte array</returns>
        public static byte[] HexToByteArray(String valueInHexadecimalFormat)
        {
            int NumberChars = valueInHexadecimalFormat.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(valueInHexadecimalFormat.Substring(i, 2), 16);
            return bytes;
        }

        /// <summary>
        /// Expects string in Ascii format
        /// </summary>
        /// <param name="valueInAsciiFormat"></param>
        /// <returns>returns byte array</returns>
        public static byte[] AsciiToByte(string valueInAsciiFormat)
        {
            return UnicodeEncoding.ASCII.GetBytes(valueInAsciiFormat);
        }

        /// <summary>
        /// Expects byte array and converts it to Hexadecimal formatted string
        /// </summary>
        /// <param name="value"></param>
        /// <returns>returns Hexadecimal formatted string</returns>
        public static string ByteArrayToHexString(byte[] value)
        {
            return BitConverter.ToString(value).Replace("-", "");
        }

        /// <summary>
        /// Expects byte array and converts it to Ascii formatted string
        /// </summary>
        /// <param name="value"></param>
        /// <returns>returns ascii formatted string</returns>
        public static string ByteArrayToAsciiString(byte[] value)
        {
            return UnicodeEncoding.ASCII.GetString(value);
        }

        /// <summary>
        /// Expects the first array to equal or smaller than the second array
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        public static byte[] XORArrays(byte[] array1, byte[] array2)
        {
            byte[] result = new byte[array1.Length];
            for (int i = 0; i < array1.Length; i++)
            {
                result[i] = (byte)(array1[i] ^ array2[i]);
            }
            return result;
        }

        /// <summary>
        /// Splits a string into sized chunks
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IEnumerable<string> EnumerateByLength(this string text, int length)
        {
            int index = 0;
            while (index < text.Length)
            {
                int charCount = Math.Min(length, text.Length - index);
                yield return text.Substring(index, charCount);
                index += length;
            }
        }

        /// <summary>
        /// Splits a string into sized chunks
        /// </summary>
        /// <param name="text"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IEnumerable<string> SplitByLength(this string text, int length)
        {
            return text.EnumerateByLength(length).ToArray();
        }

        /// <summary>
        /// Decodes an encoded hex byte array to a string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ByteArrayCodedHextoString(byte[] data)
        {
            StringBuilder result = new StringBuilder(data.Length);

            foreach (byte value in data)
            {
                // 0-1 : 0x30-0x39
                // a-f : 0x61-0x66
                // A-F : 0x41-0x46
                result.Append((char)Convert.ToInt32(value));
            }

            return result.ToString();
        }

        public static byte[] RemoveBytes(byte[] input, byte[] pattern)
        {
            if (pattern.Length == 0)
            {
                return input;
            }

            List<byte> result = new List<byte>();

            for (int i = 0; i < input.Length; i++)
            {
                bool patternLeft = i <= input.Length - pattern.Length;
                if (patternLeft && (!pattern.Where((t, j) => input[i + j] != t).Any()))
                {
                    i += pattern.Length - 1;
                }
                else
                {
                    result.Add(input[i]);
                }
            }
            return result.ToArray();
        }

        public static byte[] RemoveNonASCIIBytes(byte[] input)
        {
            int index = 0;
            byte[] output = new byte[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] >= 0x20 && input[i] <= 0x7F)
                {
                    output[index++] = input[i];
                }
            }

            Array.Resize(ref output, index);
            return output;
        }

        public static string EncodeNonAsciiCharacters(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in value)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    string encodedValue = "\\u" + ((int)c).ToString("x4");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m =>
                {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                });
        }

        public static string UnicodeToASCIIConversion(string value)
        {
            // Convert the string into a byte[].
            byte[] unicodeBytes = Encoding.Unicode.GetBytes(value);

            // Perform the conversion from one encoding to the other.
            byte[] asciiBytes = Encoding.Convert(Encoding.Unicode, Encoding.ASCII, unicodeBytes);

            // Convert the new byte[] into a char[] and then into a string.
            // This is a slightly different approach to converting to illustrate
            // the use of GetCharCount/GetChars.
            char[] asciiChars = new char[Encoding.ASCII.GetCharCount(asciiBytes, 0, asciiBytes.Length)];
            Encoding.ASCII.GetChars(asciiBytes, 0, asciiBytes.Length, asciiChars, 0);
            string asciiString = new string(asciiChars);

            return asciiString;
        }
        
        public static string UnicodeToUTF8Conversion(string value)
        {
            // Convert the string into a byte[].
            byte[] unicodeBytes = Encoding.Unicode.GetBytes(value);

            // Perform the conversion from one encoding to the other.
            byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, unicodeBytes);

            char[] utf8Chars = new char[Encoding.UTF8.GetCharCount(utf8Bytes, 0, utf8Bytes.Length)];
            Encoding.UTF8.GetChars(utf8Bytes, 0, utf8Bytes.Length, utf8Chars, 0);
            string utf8String = new string(utf8Chars);

            return utf8String;
        }
    }
}
