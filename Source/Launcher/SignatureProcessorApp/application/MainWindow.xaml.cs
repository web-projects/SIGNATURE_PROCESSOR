using Common.LoggerManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using SignatureProcessor.Processor;
using SignatureProcessorApp.application.DAL;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SignatureProcessorApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string SignatureFilename = "Signature.png";
        private static readonly string SignatureResource = "SignatureProcessorApp.application.Assets.Signature.json";
        private Storyboard blinkStoryboard;
        private bool runOnce = false;

        private const bool LOAD_FROM_RESOURCE = true;

        public MainWindow()
        {
            InitializeComponent();
            InitializeLayout();

            // Get appsettings.json config - AddEnvironmentVariables() requires package: Microsoft.Extensions.Configuration.EnvironmentVariables
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // logger manager
            SetLogging(configuration);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Start application
            if (!runOnce)
            {
                runOnce = true;
                AutoRun();
            }
        }

        private void InitializeLayout()
        {
            SetBlinkingAnimation();
            btnShow.IsEnabled = File.Exists(SignatureFilename);
        }

        private string[] GetLoggingLevels(IConfiguration configuration)
        {
            return configuration.GetSection("LoggerManager:Logging").GetValue<string>("Levels").Split("|");
        }

        private bool GetLogfileMode(IConfiguration configuration)
        {
            return configuration.GetSection("LoggerManager:Logging").GetValue<bool>("Overwrite");
        }

        private void SetLogging(IConfiguration configuration)
        {
            try
            {
                string[] logLevels = GetLoggingLevels(configuration);

                if (logLevels.Length > 0)
                {
                    string fullName = Assembly.GetEntryAssembly().Location;
                    string logname = System.IO.Path.GetFileNameWithoutExtension(fullName) + ".log";
                    string path = Directory.GetCurrentDirectory();
                    string filepath = path + "\\logs\\" + logname;

                    int levels = 0;
                    foreach (var item in logLevels)
                    {
                        foreach (var level in LogLevels.LogLevelsDictonary.Where(x => x.Value.Equals(item)).Select(x => x.Key))
                        {
                            levels += (int)level;
                        }
                    }

                    Logger.SetFileLoggerConfiguration(filepath, levels);


                    if (GetLogfileMode(configuration))
                    {
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                    }

                    Logger.info("LOGGING INITIALIZED.");

                    //Logger.info( "LOG ARG1:", "1111");
                    //Logger.info( "LOG ARG1:{0}, ARG2:{1}", "1111", "2222");
                    //Logger.debug("THIS IS A DEBUG STRING");
                    //Logger.warning("THIS IS A WARNING");
                    //Logger.error("THIS IS AN ERROR");
                    //Logger.fatal("THIS IS FATAL");
                }
            }
            catch (Exception e)
            {
                Logger.error("main: SetupLogging() - exception={0}", e.Message);
            }
        }

        private void AutoRun()
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                SignatureCapture.Dispatcher.Invoke((Action)(async () =>
                {
                    await ProcessSignatureRequest();
                }));
            });
        }

        private void LoadSignatureImageFromResource()
        {
            //List<PointCollection> pointCollection = SignatureEngine.SetLinesPointFromResource(this, SignatureResource);

            //Bitmap signatureBmp = ImageRenderer.CreateBitmapFromPoints(pointCollection);

            //string filePath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            //string fileName = System.IO.Path.Combine(filePath, SignatureFilename);

            //signatureBmp.Save(fileName, ImageFormat.Png);

            SignatureEngine.LoadSignatureImageFromResource();
        }

        private Collection<Polyline> LoadSignatureImage(Stream fileContents)
        {
            return SignatureEngine.SetLinesPointFromStream(fileContents);
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (LOAD_FROM_RESOURCE)
            {
                LoadSignatureImageFromResource();
            }
            else
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Json files (*.json)|*.json";

                if (openFileDialog.ShowDialog() == true)
                {
                    Stream fileContents = File.OpenRead(openFileDialog.FileName);
                    LoadSignatureImage(fileContents);
                }
            }
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
            _ = ProcessSignatureRequest();
        }

        private void ShowImage_Click(object sender, RoutedEventArgs e)
        {
            // Check for Button mode
            if (btnShow.Content.Equals("Show"))
            {
                //if (ImageRenderer.RenderToPNGFile(SignatureCapture, SignatureFilename))
                //{
                //    Microsoft.Win32.OpenFileDialog openFileDialong1 = new Microsoft.Win32.OpenFileDialog();
                //    openFileDialong1.Filter = "Image files (.png)|*.png";
                //    openFileDialong1.Title = "Open an Image File";
                //    openFileDialong1.ShowDialog();

                //    string fileName = openFileDialong1.FileName;

                //    if (!string.IsNullOrEmpty(fileName))
                //    {
                //        ShowImageWindow window = new ShowImageWindow();
                //        if (window.ShowImageFromFile(fileName))
                //        { 
                //            window.Show();
                //        }
                //    }
                //}

                string filePath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string fileName = System.IO.Path.Combine(filePath, SignatureFilename);
                ShowImageWindow window = new ShowImageWindow();
                if (window.ShowImageFromFile(fileName))
                {
                    window.Show();
                }
            }
            else
            {
                // Save to file
                //ImageRenderer.RenderToPNGFile(SignatureCapture, SignatureFilename);

                btnShow.Content = "Show";
                btnShow.IsEnabled = File.Exists(SignatureFilename);
            }
        }

        private async Task ProcessSignatureRequest()
        {
            SignatureCapture.Children.Clear();

            // Start Blinking
            SignatureCapture.Dispatcher.Invoke((Action)(() =>
            {
                InvalidateVisual();
                btnRun.IsEnabled = false;
                btnShow.IsEnabled = false;
                blinkStoryboard.Begin();
            }));

            DeviceProcessor deviceProcessor = new DeviceProcessor();

            // setup task
            Task task = Task.Run(() =>
            {
                MemoryStream jsonPayload = deviceProcessor.GetCardholderSignature();

                if (jsonPayload?.Length > 0)
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

                    SignatureCapture.Dispatcher.Invoke((Action)(() =>
                    {
                        btnRun.IsEnabled = true;
                        if (File.Exists(SignatureFilename))
                        {
                            btnShow.IsEnabled = true;
                            btnShow.Content = "Save";
                        }
                    }));
                });
            });
        }
    }
}
