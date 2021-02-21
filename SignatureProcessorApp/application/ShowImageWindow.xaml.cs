using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SignatureProcessor
{
    /// <summary>
    /// Interaction logic for ShowImageWindow.xaml
    /// </summary>
    public partial class ShowImageWindow : Window
    {
        public ShowImageWindow()
        {
            InitializeComponent();
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public bool ShowImageFromFile(string filename)
        {
            try
            {
                //here you create a bitmap image from filename
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(filename);
                bitmap.EndInit();

                ImageBrush ib = new ImageBrush();
                ib.ImageSource = new BitmapImage(new Uri(filename, UriKind.Relative));
                SignatureCapture.Background = ib;

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"There was an error saving the file: {ex.Message}");
                return false;
            }
        }
    }
}