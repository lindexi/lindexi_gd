using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace BuceafalfeNelnujellel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            const int width = 1;
            const int height = width;
            const double dpi = 96;
            // R G B 三个像素
            const int colorLength = 3;

            var image = BitmapSource.Create(width, height, dpi, dpi, PixelFormats.Bgr24, null, new byte[colorLength],
                colorLength);

            foreach (var (name, encoder) in new (string, BitmapEncoder)[]
            {
                (".png", new PngBitmapEncoder()),
                (".jpg", new JpegBitmapEncoder()),
                (".bmp", new BmpBitmapEncoder()),
                (".gif", new GifBitmapEncoder()),
            })
            {
                var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + name);

                WriteImage(encoder, image, fileName);

                // 等待写入磁盘
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();

                var byteList = File.ReadAllBytes(fileName);

                Debug.WriteLine($"{name} byte count = {byteList.Length}");
            }
        }

        private static void WriteImage(BitmapEncoder encoder, BitmapSource image, string fileName)
        {
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (Stream stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }
    }
}