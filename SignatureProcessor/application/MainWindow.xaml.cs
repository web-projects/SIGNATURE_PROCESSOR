using Microsoft.Win32;
using SignatureProcessor.application.DAL;
using SignatureProcessor.Processor;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SignatureProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string SignatureResource = "SignatureProcessor.Assets.Signature.json";
        private Storyboard blinkStoryboard;

        public MainWindow()
        {
            InitializeComponent();
            SetBlinkingAnimation();
        }

        private void LoadSignatureImage()
        {
            SignatureEngine.DrawLinesPointFromResource(this, SignatureResource);
        }

        private Collection<Polyline> LoadSignatureImage(Stream fileContents)
        {
            return SignatureEngine.DrawLinesPointFromStream(fileContents);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json files (*.json)|*.json";

            if (openFileDialog.ShowDialog() == true)
            {
                Stream fileContents = File.OpenRead(openFileDialog.FileName);
                LoadSignatureImage(fileContents);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            ImageRenderer.RenderToPNGFile(SignatureCapture, "signature.png");
        }

        private void SetBlinkingAnimation()
        {
            var blinkAnimation = new DoubleAnimationUsingKeyFrames();
            blinkAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            blinkAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));

            blinkStoryboard = new Storyboard
            {
                Duration = TimeSpan.FromMilliseconds(500),
                RepeatBehavior = RepeatBehavior.Forever,
            };

            Storyboard.SetTarget(blinkAnimation, SignatureCapture);
            Storyboard.SetTargetProperty(blinkAnimation, new PropertyPath(OpacityProperty));

            blinkStoryboard.Children.Add(blinkAnimation);
        }

        private void GetSignature_Click(object sender, RoutedEventArgs e)
        {
            SignatureCapture.Children.Clear();

            this.Dispatcher.Invoke((Action)(() =>
            {
                SignatureCapture.InvalidateVisual();
                // Start Blinking
                blinkStoryboard.Begin();
            }));

            this.Dispatcher.Invoke(delegate
            {
                DeviceProcessor deviceProcessor = new DeviceProcessor();
                MemoryStream jsonPayload = deviceProcessor.GetCardholderSignature();
                if (jsonPayload.Length > 0)
                {
                    Collection<Polyline> collection = LoadSignatureImage(jsonPayload);
                    foreach (var child in collection)
                    {
                        SignatureCapture.Children.Add(child);
                    }
                }
                deviceProcessor.Dispose();

                // Stop blinking
                blinkStoryboard.Stop();
            });

            // create a thread  
            //Thread newWindowThread = new Thread(new ThreadStart(() =>
            //{
            //    DeviceProcessor deviceProcessor = new DeviceProcessor();
            //    MemoryStream jsonPayload = deviceProcessor.GetCardholderSignature();
            //    if (jsonPayload.Length > 0)
            //    {
            //        Collection<Polyline> collection = LoadSignatureImage(jsonPayload);
            //        foreach (var child in collection)
            //        {
            //            this.Dispatcher.Invoke((Action)(() =>
            //            {
            //                SignatureCapture.Children.Add(child);
            //            }));
            //        }
            //    }
            //    deviceProcessor.Dispose();

            //    // start the Dispatcher processing  
            //    System.Windows.Threading.Dispatcher.Run();
            //}));

            //// set the apartment state  
            //newWindowThread.SetApartmentState(ApartmentState.STA);

            //// make the thread a background thread  
            //newWindowThread.IsBackground = true;

            //// start the thread  
            //newWindowThread.Start();
        }
    }
}
