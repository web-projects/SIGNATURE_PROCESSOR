using Devices.SignatureProcessor;
using Microsoft.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;

namespace SignatureProcessor.Processor
{
    public static class ImageRenderer
    {
        private const double defaultDpi = 96.0;
        private static readonly RecyclableMemoryStreamManager recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

        public static Bitmap CreateBitmapFromPoints(List<PointCollection> pointCollection)
        {
            Bitmap signatureBmp = new Bitmap(400, 300);
            Graphics flagGraphics = Graphics.FromImage(signatureBmp);

            // Create pen.
            System.Drawing.Pen redPen = new System.Drawing.Pen(System.Drawing.Color.Red, 1);

            foreach (var points in pointCollection)
            {
                PointF[] signaturePoints = new PointF[points.Count];
                int index = 0;
                foreach (var point in points)
                {
                    object value = new PointF((float)point.X, (float)point.Y);
                    signaturePoints.SetValue(value, index++);
                }
                flagGraphics.DrawPolygon(redPen, signaturePoints);
            }

            return signatureBmp;
        }

        public static void CreateImageFromStream(byte[] imageBytes)
        {
            // enable only for testing purposes
            using MemoryStream memoryStream = recyclableMemoryStreamManager.GetStream();
            Image signatureImage = Image.FromStream(memoryStream);
            signatureImage.Save(Path.Combine("C:\\Temp", SignatureParameters.M400SignatureFilename), ImageFormat.Png);
        }
    }
}
