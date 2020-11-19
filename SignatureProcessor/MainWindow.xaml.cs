using SignatureProcessor.Processor;
using System.Windows;

namespace SignatureProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string SignatureFile = "SignatureProcessor.Assets.Signature.json";

        public MainWindow()
        {
            InitializeComponent();
            LoadSignatureImage();
        }

        private void LoadSignatureImage()
        {
            SignatureEngine.DrawLinesPoint(this, SignatureFile);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ImageRenderer.RenderToPNGFile(SignatureCapture, "signature.png");
        }
    }
}
