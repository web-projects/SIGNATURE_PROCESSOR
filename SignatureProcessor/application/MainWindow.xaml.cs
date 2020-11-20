using Microsoft.Win32;
using SignatureProcessor.application.DAL;
using SignatureProcessor.Processor;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
            SignatureEngine.SetLinesPointFromResource(this, SignatureResource);
        }

        private Collection<Polyline> LoadSignatureImage(Stream fileContents)
        {
            return SignatureEngine.SetLinesPointFromStream(fileContents);
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

        private async void GetSignature_Click(object sender, RoutedEventArgs e)
        {
            SignatureCapture.Children.Clear();

            // Start Blinking
            SignatureCapture.Dispatcher.Invoke((Action)(() =>
            {
                InvalidateVisual();
                blinkStoryboard.Begin();
            }));

            DeviceProcessor deviceProcessor = new DeviceProcessor();

            // setup task
            Task task = Task.Run(() =>
            {
                MemoryStream jsonPayload = deviceProcessor.GetCardholderSignature();

                if (jsonPayload.Length > 0)
                {
                    SignatureCapture.Dispatcher.Invoke(() =>
                    {
                        Debug.WriteLine("COLLECTING POINTS...");
                        Collection<Polyline> collection = LoadSignatureImage(jsonPayload);
                        
                        foreach (var child in collection)
                        {
                            SignatureCapture.Children.Add(child);
                        }

                        Debug.WriteLine("COLLECTING POINTS...DONE!");
                    });
                }
            });

            // clean up task
            await task.ContinueWith(async (t1) =>
            {
                // Release device and stop blinking
                await Task.Run(() =>
                {
                    Debug.WriteLine("DISPOSING...");
                    deviceProcessor?.Dispose();
                    blinkStoryboard.Stop();
                    Debug.WriteLine("DISPOSING...DONE!");
                });
            });
        }
    }
}
