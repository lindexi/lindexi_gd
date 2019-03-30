using System;
using System.Collections.Generic;
using System.Drawing;
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
using RaiwairwofayfuHeehenagelki.GifImage;

namespace RaiwairwofayfuHeehenagelki
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Task.Run(async () =>
            {
                var gifImage =
                await Dispatcher.InvokeAsync(() => GifDecoder.Decode("1.gif"));
                 foreach (var temp in gifImage.Frames)
                 {
                     Dispatcher.Invoke(() =>
                     {
                         //Image.Source = BitmapToImageSource(temp.Image);
                         Image.Source = temp.FearjallgarhifarFecheakabeli;
                     });
                    await Task.Delay(TimeSpan.FromMilliseconds(temp.Delay * 10));
                 }
            });
        }

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }
    }
}
