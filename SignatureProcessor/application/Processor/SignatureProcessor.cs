using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SignatureProcessor.Processor
{
    public static class SignatureEngine
    {
        // for visualization of different signature strokes
        private static List<Brush> brushes = new List<Brush>()
        {
            { Brushes.Red },
            { Brushes.Blue },
            { Brushes.Green },
            { Brushes.Cyan },
            { Brushes.Yellow }
        };

        public static List<PointCollection> SetLinesPointFromResource(MainWindow window, string signatureFile)
        {
            // Process signature payload
            SignatureLoader signatureLoader = new SignatureLoader();
            signatureLoader.LoadJsonFromResource(signatureFile);
            List<PointCollection> pointCollection = signatureLoader.GetSignaturePoints();

            // Draw lines to screen.
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
                window.SignatureCapture.Children.Add(line);
            }

            return pointCollection;
        }

        public static Collection<Polyline> SetLinesPointFromStream(Stream signatureFile)
        {
            // Process signature payload
            SignatureLoader signatureLoader = new SignatureLoader();
            signatureLoader.LoadJsonFromStream(signatureFile);

            int index = 0;
            List<PointCollection> pointCollection = signatureLoader.GetSignaturePoints();
            Collection< Polyline> children = new Collection<Polyline>();

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
