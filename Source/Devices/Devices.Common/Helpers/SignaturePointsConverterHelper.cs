using System;

namespace SignatureProcessorApp.devices.common.Helpers
{
    public static class SignaturePointsConverterHelper
    {
        // patthern: {"t":0,"x":-1,"y":-1}
        public static readonly byte[] SignatureEndOfStroke = new byte[] { 0x7b, 0x22, 0x74, 0x22, 0x3a, 0x30, 0x2c, 0x22, 0x78, 0x22, 0x3a, 0x2d, 0x31, 0x2c, 0x22, 0x79, 0x22, 0x3a, 0x2d, 0x31, 0x7d };

        //public static List<PointF[]> FormatPointsForBitmap(List<SignatureObject> signaturePoints)
        //{
        //    int index = 0;
        //    int totalIndex = 0;
        //    List<PointF[]> signatureBitmapPoints = new List<PointF[]>(signaturePoints.Count);
        //    PointF[] bitmapPoints = new PointF[signaturePoints.Count];
        //    foreach (var point in signaturePoints)
        //    {
        //        if (point.x == -1 && point.x == -1)
        //        {
        //            PointF[] fields = new PointF[index];
        //            Array.Copy(bitmapPoints, fields, index);
        //            bitmapPoints = new PointF[signaturePoints.Count - totalIndex];
        //            index = 0;
        //            signatureBitmapPoints.Add(fields);
        //            continue;
        //        }
        //        object value = new PointF(point.x, point.y);
        //        bitmapPoints.SetValue(value, index++);
        //        totalIndex++;
        //    }

        //    return signatureBitmapPoints;
        //}

        //public static byte[] ConvertPointsToImage(byte[] signaturePointsInBytes)
        //{
        //    string json = ConversionHelper.ByteArrayCodedHextoString(signaturePointsInBytes);

        //    // look for closing bracket
        //    json = json.Substring(0, json.LastIndexOf(']') + 1);
        //    List<SignatureObject> signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(json);
        //    List<PointF[]> pointCollection = FormatPointsForBitmap(signaturePoints);

        //    Bitmap signatureBmp = ImageRenderer.CreateBitmapFromPoints(pointCollection);

        //    using (var stream = new MemoryStream())
        //    {
        //        signatureBmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        //        return stream.ToArray();
        //    }
        //}

        public static byte[] PruneByteArray(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return bytes;
            }

            int i = bytes.Length - 1;

            while (bytes[i] == 0)
            {
                i--;
            }

            byte[] copy = new byte[i + 1];
            Array.Copy(bytes, copy, i + 1);

            return copy;
        }

        public static byte[] RemoveTrailingPattern(byte[] input, byte[] pattern)
        {
            int index = 0;
            int offset = input.Length - pattern.Length - 1;

            foreach (byte element in pattern)
            {
                if (element != input[offset + index++])
                {
                    return input;
                }
            }

            byte[] updatedArray = new byte[input.Length - index - 1];
            Array.Copy(input, 0, updatedArray, 0, input.Length - index - 2);
            updatedArray[updatedArray.Length - 1] = 0x5D;

            return PruneByteArray(updatedArray);
        }
    }
}
