using Devices.Common.Helpers.Templates;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Devices.Verifone.VIPA.TagLengthValue
{
    /// <summary>
    /// This class is used mostly for debugging purpose only (printing to console) and have no impact on main workflow
    /// </summary>
    internal class TLVEncoding
    {
        internal enum TLVDataEncoding
        {
            ASCII,
            Binary,
            Decimal
        }

        private static readonly List<TLVEncoding> TLVEncodings = new List<TLVEncoding>
        {
            new TLVEncoding(EETemplate.TerminalId,nameof(EETemplate.TerminalId), TLVDataEncoding.ASCII),
            new TLVEncoding(EETemplate.SerialNumber,nameof(EETemplate.SerialNumber), TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDFA21D , "MaximumChainedCommandsSize", TLVDataEncoding.Decimal),
            new TLVEncoding( 0xEE , "TerminalInfo"),
            new TLVEncoding( 0xDF0D , "Name", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF7F , "Version", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xEF , "VersionInfo"),
            new TLVEncoding( 0xDF8106 , "LibraryName", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF8107 , "LibraryVersion", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF8108 , "Architecture", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF8109 , "ArchitectureType", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDFEC1F , "PowerUpTimeSec", TLVDataEncoding.Decimal),
            new TLVEncoding( 0xDF8101 , "TamperStatus"),
            new TLVEncoding( 0xDF8102 , "ARSStatus"),
            new TLVEncoding( 0xF0 , "PinPad"),
            new TLVEncoding( 0xDF8111 , "PinPadStatus"),
            new TLVEncoding( 0xDF810A , "ModelNumber", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF810B , "HardwareVersion", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF810C , "PartNumber", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF810D , "OSNumber", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF810E , "AQUILAVersion", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF810F , "IPPMiddlewareVersion", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDF8110 , "PermanentUnitSerialNumber", TLVDataEncoding.ASCII),
            new TLVEncoding( 0xDFA202 , "OptionID"),
            new TLVEncoding( 0xDFED14 , "VSS Script Filename", TLVDataEncoding.ASCII)
        };

        private TLVEncoding()
        {
        }

        private TLVEncoding(uint tag, string tagName, TLVDataEncoding encoding = TLVDataEncoding.Binary)
        {
            this.Tag = tag;
            this.Name = tagName;
            this.Encoding = encoding;
        }

        public uint Tag { get; set; }
        public string Name { get; set; } = string.Empty;
        public TLVDataEncoding Encoding { get; set; } = TLVDataEncoding.Binary;
        public bool EMVData { get; set; } = false;

        public static void PrintTags(List<TLV> tags, int indent = 0)
        {
            if (tags == null)
            {
                return;
            }

            string indentString = new string('\t', indent);

            foreach (TLV tag in tags)
            {
                TLVEncoding encoding = new TLVEncoding
                {
                    Name = $"Unknown-{tag.Tag}"
                };

                foreach (TLVEncoding tagEncoding in TLVEncodings)
                {
                    if (tag.Tag == tagEncoding.Tag)
                    {
                        encoding = tagEncoding;
                        break;
                    }
                }
                if (tag.InnerTags != null)
                {
#if DEBUG
                    Console.WriteLine($"{indentString}{encoding.Name}:");
#endif
                    PrintTags(tag.InnerTags, indent + 1);
                }
                else
                {
                    string value = BitConverter.ToString(tag.Data);

                    if (encoding.Encoding == TLVDataEncoding.ASCII)
                    {
                        value = System.Text.Encoding.UTF8.GetString(tag.Data);
                    }
                    else if (encoding.Encoding == TLVDataEncoding.Decimal)
                    {
                        decimal decimalValue = 0;

                        foreach (var byt in tag.Data)
                        {
                            decimalValue *= 256;
                            decimalValue += byt;
                        }

                        value = decimalValue.ToString(CultureInfo.InvariantCulture);
                    }
#if DEBUG
                    Console.WriteLine($"{indentString}{encoding.Name}: {value}");
#endif
                }
            }
        }
    }
}
