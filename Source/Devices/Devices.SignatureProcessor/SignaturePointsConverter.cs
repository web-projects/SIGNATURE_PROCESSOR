﻿using Common.LoggerManager;
using Devices.Common.Helpers;
using Devices.Common.SignatureProcessor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Devices.SignatureProcessor
{
    public static class SignaturePointsConverter
    {
        public static List<PointF[]> FormatPointsForBitmap(List<SignatureObject> signaturePoints)
        {
            int index = 0;
            int totalIndex = 0;
            List<PointF[]> signatureBitmapPoints = new List<PointF[]>(signaturePoints.Count);
            PointF[] bitmapPoints = new PointF[signaturePoints.Count];
            foreach (var point in signaturePoints)
            {
                if (point.x == -1 && point.x == -1)
                {
                    PointF[] fields = new PointF[index];
                    Array.Copy(bitmapPoints, fields, index);
                    bitmapPoints = new PointF[signaturePoints.Count - totalIndex];
                    index = 0;
                    signatureBitmapPoints.Add(fields);
                    continue;
                }
                object value = new PointF(point.x, point.y);
                bitmapPoints.SetValue(value, index++);
                totalIndex++;
            }

            return signatureBitmapPoints;
        }

        public static byte[] ConvertPointsToImage(byte[] signaturePointsInBytes)
        {
            string json = ConversionHelper.ByteArrayCodedHextoString(signaturePointsInBytes);
            Logger.debug($"{json}");

            // look for closing bracket
            string jsonPayload = json.Substring(0, json.LastIndexOf(']') + 1);

            try
            {
                List<SignatureObject> signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(jsonPayload);
                List<PointF[]> pointCollection = FormatPointsForBitmap(signaturePoints);

                Bitmap signatureBmp = ImageRenderer.CreateBitmapFromPoints(pointCollection);

                using (var stream = new MemoryStream())
                {
                    signatureBmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception converting signature points: {ex.Message}");
                Logger.error($"Exception converting signature points: {ex.Message}");
            }

            return null;
        }

        public static byte[] PruneByteArray(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return bytes;
            }

            int i = bytes.Length - 1;

            // remove non-printable characters in the stream
            while (bytes[i] <= 0x20 || bytes[1] >= 0x7F)
            {
                i--;
            }

            int arrayLen = i + 1;
            byte[] copy = new byte[arrayLen];

            Array.Copy(bytes, copy, arrayLen);
            Array.Resize<byte>(ref copy, arrayLen);

            return copy;
        }
    }
}
