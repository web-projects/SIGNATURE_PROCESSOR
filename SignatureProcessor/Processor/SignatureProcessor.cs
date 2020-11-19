﻿using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SignatureProcessor.Processor
{
    public static class SignatureEngine
    {
        // for visualization of differnt signature strokes
        private static List<Brush> brushes = new List<Brush>()
        {
            { Brushes.Red },
            { Brushes.Blue },
            { Brushes.Green },
            { Brushes.Cyan },
            { Brushes.Yellow }
        };

        public static void DrawLinesPointFromResource(MainWindow window, string signatureFile)
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
                    line.Points.Add(point);
                }
                window.SignatureCapture.Children.Add(line);
            }
        }

        public static void DrawLinesPointFromStream(MainWindow window, Stream signatureFile)
        {
            window.SignatureCapture.Children.Clear();

            // Process signature payload
            SignatureLoader signatureLoader = new SignatureLoader();
            signatureLoader.LoadJsonFromStream(signatureFile);

            int index = 0;
            List<PointCollection> pointCollection = signatureLoader.GetSignaturePoints();

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
                window.SignatureCapture.Children.Add(line);
            }
        }
    }
}
