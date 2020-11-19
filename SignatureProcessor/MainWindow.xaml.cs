using Microsoft.Win32;
using SignatureProcessor.Processor;
using System.IO;
using System.Windows;

namespace SignatureProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string SignatureResource = "SignatureProcessor.Assets.Signature.json";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadSignatureImage()
        {
            SignatureEngine.DrawLinesPointFromResource(this, SignatureResource);
        }

        private void LoadSignatureImage(Stream fileContents)
        {
            SignatureEngine.DrawLinesPointFromStream(this, fileContents);
        }

        private void OpneFile_Click(object sender, RoutedEventArgs e)
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
    }
}
