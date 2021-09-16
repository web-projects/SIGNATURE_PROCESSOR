using System;
using System.Collections.Generic;
using System.Linq;

namespace Devices.Verifone.VIPA.TagLengthValue
{
    public class TLV
    {
        public TLV()            //This will be used for more complex ones (with inner tags)
        {
        }

        public TLV(byte[] tag, byte[] data) //For simple ones (no inner tags)
        {
            this.Tag = CombineByteArray(tag);
            this.Data = data;
        }

        public TLV(uint tag, params byte[] data) //For simple ones (no inner tags)
        {
            this.Tag = tag;
            this.Data = data;
        }

        public uint Tag { get; set; }

        public byte[] Data { get; set; }     // Value should be null if innerTags is populated

        public List<TLV> InnerTags { get; set; } // Value should be null if data is populated

        public static List<TLV> Decode(byte[] data, int startoffset = 0, int dataLength = -1, params uint[] tagofTagsArray)
        {
            if (data == null)
            {
                return null;
            }

            List<TLV> allTags = new List<TLV>();
            int dataOffset = startoffset;

            if (dataLength == -1)
            {
                dataLength = data.Length;
            }

            tagofTagsArray ??= Array.Empty<uint>();

            while (dataOffset < dataLength)
            {
                int tagStartOffset = dataOffset;
                byte tagByte0 = data[dataOffset];
                int tagLength = 1;

                if ((tagByte0 & 0x1F) == 0x1F)
                {
                    // Long form tag
                    dataOffset++;       // Skip first tag byte

                    while ((data[dataOffset] & 0x80) == 0x80)
                    {
                        tagLength++;   // More bit set, so add middle byte to tagLength
                        dataOffset++;
                    }

                    tagLength++;       // Include final byte (where more=0)
                    dataOffset++;
                }
                else
                {
                    // Short form (single byte) tag
                    dataOffset++;       // Simply increment past single byte tag; tagLength is already 1
                }

                // protect from buffer overrun
                if (dataOffset >= data.Length)
                {
                    return null;
                }

                byte lengthByte0 = data[dataOffset];
                int tagDataLength = 0;

                if ((lengthByte0 & 0x80) == 0x80)
                {
                    // Long form length
                    int tagDataLengthLength = lengthByte0 & 0x7F;
                    int tagDataLengthIndex = 0;
                    while (tagDataLengthIndex < tagDataLengthLength)
                    {
                        tagDataLength <<= 8;
                        tagDataLength += data[dataOffset + tagDataLengthIndex + 1];

                        tagDataLengthIndex++;
                    }

                    dataOffset += 1 + tagDataLengthLength;  // Skip long form byte, plus all length bytes
                }
                else
                {
                    // Short form (single byte) length
                    tagDataLength = lengthByte0;
                    dataOffset++;
                }

                TLV tag = new TLV
                {
                    Tag = CombineByteArray(new ReadOnlySpan<byte>(data, tagStartOffset, tagLength))
                };

                if (tagofTagsArray.Contains(tag.Tag))
                {
                    tag.InnerTags = Decode(data, dataOffset, dataOffset + tagDataLength, tagofTagsArray);
                }
                else
                {
                    // special case for signature capture
                    if (tag.Tag == 0xdfaa03)
                    {
                        tagDataLength *= 2;
                    }
                    
                    // special handling of POS cancellation: "ABORTED" is in the data field without a length
                    if (tagDataLength > data.Length)
                    {
                        tagDataLength = data.Length;
                        dataOffset = 0;
                    }

                    tag.Data = new byte[tagDataLength];
                    Array.Copy(data, dataOffset, tag.Data, 0, tagDataLength);
                }

                allTags.Add(tag);

                dataOffset += tagDataLength;
            }

            return /*(allTags.Count == 0) ? null :*/ allTags;
        }

        //When you want to automatically decode more than 1 layer of innertags
        public static List<TLV> DeepDecode(byte[] data, int count = 0)
        {
            if (count < 0)      //don't go deeper than needed
            {
                return null;
            }
            List<TLV> firstLayer = Decode(data);
            if (firstLayer == null || firstLayer.Count == 0)      //If this is no longer decodable, don't bother going deeper
            {
                return null;
            }

            foreach (TLV nextLayer in firstLayer)
            {
                nextLayer.InnerTags = DeepDecode(nextLayer.Data, count - 1);
            }

            return firstLayer;
        }

        public static byte[] Encode(TLV tags)
        {
            return Encode(new List<TLV> { tags });
        }

        public static byte[] Encode(List<TLV> tags)
        {
            List<byte[]> allTagBytes = new List<byte[]>();
            int allTagBytesLength = 0;

            foreach (TLV tag in tags)
            {
                byte[] tagRaw = SplitUIntToByteArray(tag.Tag);
                int len = tagRaw.Length;
                byte[] data = tag.Data;

                if (tag.InnerTags != null)
                {
                    data = Encode(tag.InnerTags);
                }

                if (data == null)
                {
                    data = Array.Empty<byte>();
                }

                if (data.Length > 65535)
                {
                    throw new Exception($"TLV data too long for Encode: length {data.Length}");
                }

                if (data.Length > 255)
                {
                    len += 3 + data.Length;
                }
                else if (data.Length > 127)
                {
                    len += 2 + data.Length;
                }
                else
                {
                    len += 1 + data.Length;
                }

                byte[] tagData = new byte[len];
                int tagDataOffset = 0;

                Array.Copy(tagRaw, 0, tagData, tagDataOffset, tagRaw.Length);
                tagDataOffset += tagRaw.Length;

                if (data.Length > 255)
                {
                    tagData[tagDataOffset + 0] = 0x80 + 2;

                    tagData[tagDataOffset + 1] = (byte)(data.Length / 256);

                    tagData[tagDataOffset + 2] = (byte)(data.Length % 256);

                    tagDataOffset += 3;
                }
                else if (data.Length > 127)
                {
                    tagData[tagDataOffset + 0] = 0x80 + 1;

                    tagData[tagDataOffset + 1] = (byte)data.Length;

                    tagDataOffset += 2;
                }
                else
                {
                    tagData[tagDataOffset] = (byte)data.Length;
                    tagDataOffset += 1;
                }

                Array.Copy(data, 0, tagData, tagDataOffset, data.Length);
                tagDataOffset += data.Length;

                allTagBytes.Add(tagData);
                allTagBytesLength += tagDataOffset;
            }

            byte[] allBytes = new byte[allTagBytesLength];
            int allBytesOffset = 0;

            foreach (byte[] tagBytes in allTagBytes)
            {
                Array.Copy(tagBytes, 0, allBytes, allBytesOffset, tagBytes.Length);
                allBytesOffset += tagBytes.Length;
            }

            return allBytes;
        }

        private static byte[] SplitUIntToByteArray(uint value)
        {
            //This is a lot faster than doing a loop (3s for 10M ops vs 5s for 10M ops using loops)
            if (value <= 0xFF)
                return new byte[] { (byte)value };
            else if (value <= 0xFFFF)
                return new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
            else if (value <= 0xFFFFFF)
                return new byte[] { (byte)(value >> 16), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF) };

            return new byte[] { (byte)(value >> 24), (byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF) };
        }

        private static uint CombineByteArray(ReadOnlySpan<byte> span)
        {
            uint result = 0;
            for (int i = 0; i < span.Length; i++)
            {
                result += (uint)(span[i] << ((span.Length - i - 1) * 8));
            }

            return result;
        }
    }
}
