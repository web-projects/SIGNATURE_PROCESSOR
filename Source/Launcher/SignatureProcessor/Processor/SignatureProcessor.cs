using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace SignatureProcessor.Processor
{
    public static class SignatureEngine
    {
        private static readonly string SignatureFilename = "Signature.png";
        private static readonly string SignatureResource = "SignatureProcessor.Assets.Signature.json";

        // for visualization of different signature strokes
        private static List<Brush> brushes = new List<Brush>()
        {
            { Brushes.Red },
            { Brushes.Blue },
            { Brushes.Green },
            { Brushes.Cyan },
            { Brushes.Yellow }
        };

        public static void LoadSignatureImageFromResource()
        {
            List<PointCollection> pointCollection = SetLinesPointFromResource(SignatureResource);

            Bitmap signatureBmp = ImageRenderer.CreateBitmapFromPoints(pointCollection);

            string filePath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string fileName = System.IO.Path.Combine(filePath, SignatureFilename);

            signatureBmp.Save(fileName, ImageFormat.Png);
        }

        private static List<PointCollection> SetLinesPointFromResource(string signatureFile)
        {
            // Process signature payload
            SignaturePointsConverter signatureLoader = new SignaturePointsConverter();
            signatureLoader.LoadJsonFromResource(signatureFile);
            List<PointCollection> pointCollection = signatureLoader.GetSignaturePoints();

            // Set lines to draw
            Polyline line = new Polyline();
            line.Stroke = Brushes.Red;
            line.StrokeThickness = 1;

            foreach (var points in pointCollection)
            {
                foreach (var point in points)
                {
                    if (point.X != -1 && point.Y != -1)
                    {
                        line.Points.Add(point);
                    }
                }
            }

            return pointCollection;
        }

        public static Collection<Polyline> SetLinesPointFromStream(Stream signatureFile)
        {
            // Process signature payload
            SignaturePointsConverter signatureLoader = new SignaturePointsConverter();
            signatureLoader.LoadJsonFromStream(signatureFile);

            int index = 0;
            List<PointCollection> pointCollection = signatureLoader.GetSignaturePoints();
            Collection<Polyline> children = new Collection<Polyline>();

            foreach (var points in pointCollection)
            {
                // Draw lines to screen
                Polyline line = new Polyline();
                line.Stroke = brushes[index++];

                // for visualization of differnt signature strokes
                if (index > brushes.Count - 1)
                {
                    index = 0;
                }

                line.StrokeThickness = 1;
                foreach (var point in points)
                {
                    line.Points.Add(point);
                }
                children.Add(line);
            }

            return children;
        }
    }
}
