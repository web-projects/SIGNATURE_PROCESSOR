using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SignatureProcessor.Processor
{
    public static class ImageRenderer
    {
        private const double defaultDpi = 96.0;

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
                foreach (System.Windows.Point point in points)
                {
                    object value = new PointF((float)point.X, (float)point.Y);
                    signaturePoints.SetValue(value, index ++);
                }
                flagGraphics.DrawPolygon(redPen, signaturePoints);
            }

            return signatureBmp;
        }

        public static ImageSource RenderToPNGImageSource(Visual targetControl)
        {
            var renderTargetBitmap = GetRenderTargetBitmapFromControl(targetControl);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            var result = new BitmapImage();

            using (var memoryStream = new MemoryStream())
            {
                encoder.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = memoryStream;
                result.EndInit();
            }

            return result;
        }

        public static bool RenderToPNGFile(Visual targetControl, string filename)
        {
            BitmapSource renderTargetBitmap = GetRenderTargetBitmapFromControl(targetControl);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            var result = new BitmapImage();

            try
            {
                using (var fileStream = new FileStream(filename, FileMode.Create))
                {
                    encoder.Save(fileStream);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"There was an error saving the file: {ex.Message}");
                return false;
            }
        }

        private static BitmapSource GetRenderTargetBitmapFromControl(Visual targetControl, double dpi = defaultDpi)
        {
            if (targetControl == null)
            {
                return null;
            }

            Rect bounds = VisualTreeHelper.GetDescendantBounds(targetControl);

            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)(bounds.Width * dpi / 96.0),
                                                                           (int)(bounds.Height * dpi / 96.0),
                                                                           dpi,
                                                                           dpi,
                                                                           PixelFormats.Pbgra32);

            DrawingVisual drawingVisual = new DrawingVisual();

            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                VisualBrush visualBrush = new VisualBrush(targetControl);
                drawingContext.DrawRectangle(visualBrush, null, new Rect(new System.Windows.Point(), bounds.Size));
            }

            renderTargetBitmap.Render(drawingVisual);

            return renderTargetBitmap;
        }
    }
}
