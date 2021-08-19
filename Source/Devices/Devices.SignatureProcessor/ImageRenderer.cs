using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Devices.Common.SignatureProcessor
{
    public static class ImageRenderer
    {
        private static readonly string SignatureFilename = "Signature.png";

        public static Bitmap CreateBitmapFromPoints(List<PointF[]> signaturePoints)
        {
            Bitmap signatureBmp = new Bitmap(800, 480);
            Graphics flagGraphics = Graphics.FromImage(signatureBmp);

            // Create pen.
            Pen blackPen = new Pen(Color.Black, 1);

            foreach (var point in signaturePoints)
            {
                flagGraphics.DrawPolygon(blackPen, point);
            }

            return signatureBmp;
        }

        public static void CreateImageFromStream(byte[] imageBytes)
        {
            Stream stream = new MemoryStream(imageBytes);
            Image signatureImage = Image.FromStream(stream);
            signatureImage.Save(Path.Combine("C:\\Temp", SignatureFilename), ImageFormat.Png);
        }
    }
}
