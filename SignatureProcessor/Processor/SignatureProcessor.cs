using System.Windows.Media;
using System.Windows.Shapes;

namespace SignatureProcessor.Processor
{
    public static class SignatureEngine
    {
        public static void DrawLinesPoint(MainWindow window, string signatureFile)
        {
            // Process signature payload
            SignatureLoader signatureLoader = new SignatureLoader();
            signatureLoader.LoadJson(signatureFile);
            PointCollection points = signatureLoader.GetSignaturePoints();

            // Draw lines to screen.
            Polyline line = new Polyline();
            line.Stroke = Brushes.Red;
            line.StrokeThickness = 1;
            foreach (var point in points)
            {
                line.Points.Add(point);
            }
            window.SignatureCapture.Children.Add(line);
        }
    }
}
