using System;
using System.Collections.Generic;
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

namespace KohaykowurchemJaibuqajijiyeco
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SizeChanged += MainWindow_SizeChanged;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var list = StringNumberToList();

            ToBitmap(list);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var list = StringNumberToList();

            ToBitmap(list);
        }

        private unsafe void ToBitmap(List<int> list)
        {
            var width = ActualWidth;
            var height = ActualHeight;
            if (width < 1 || height < 1)
            {
                return;
            }

            width = 200;
            height = 200;

            var writeableBitmap = ToBitmap(list, width, height);

            //writeableBitmap = ScalePixel(writeableBitmap);

            Image.Source = writeableBitmap;

            //width *= 3;
            //height *= 3;

            Image.Width = width;
            Image.Height = height;
        }

        private unsafe WriteableBitmap ScalePixel(WriteableBitmap original)
        {
            var scale = 2;

            var newBitmap = new WriteableBitmap(original.PixelWidth * scale, original.PixelWidth * scale, 96, 96,
                PixelFormats.Bgra32, null);

            original.Lock();
            newBitmap.Lock();
            var length = original.PixelWidth * original.PixelHeight *
                original.Format.BitsPerPixel / 8;

            var backBuffer = (byte*)original.BackBuffer;
            var newBackBuffer = (byte*)newBitmap.BackBuffer;

            for (int i = 0; i < original.PixelHeight; i++)
            {
                var pixelByteCount = original.Format.BitsPerPixel / 8;
                var width = original.PixelWidth * pixelByteCount;
                for (int j = 0; j + pixelByteCount < width; j += pixelByteCount)
                {
                    var originPixel = i * width + j;

                    var blue = backBuffer[originPixel];
                    var green = backBuffer[originPixel + 1];
                    var red = backBuffer[originPixel + 2];
                    var alpha = backBuffer[originPixel + 3];

                    for (int k = 0; k < scale; k++)
                    {
                        for (int l = 0; l < scale; l++)
                        {
                            var newPixel = (i * scale + k) * width + j * scale * l;//* pixelByteCount;
                            CopyPixel(newPixel);
                            CopyPixel(newPixel + pixelByteCount);
                        }
                    }


                    //newPixel = (i * scale + 1) * width + j * scale;
                    //CopyPixel(newPixel);
                    //CopyPixel(newPixel + 4);

                    void CopyPixel(int startNumber)
                    {
                        newBackBuffer[startNumber] = blue;
                        newBackBuffer[startNumber + 1] = green;
                        newBackBuffer[startNumber + 2] = red;
                        newBackBuffer[startNumber + 3] = alpha;
                    }
                }
            }

            newBitmap.AddDirtyRect(new Int32Rect(0, 0, newBitmap.PixelWidth, newBitmap.PixelHeight));
            newBitmap.Unlock();
            original.Unlock();

            return newBitmap;
        }

        private static unsafe WriteableBitmap ToBitmap(List<int> list, double width, double height)
        {
            var writeableBitmap = new WriteableBitmap((int)width, (int)height, 96, 96, PixelFormats.Bgra32, null);

            writeableBitmap.Lock();

            var backBuffer = (byte*)writeableBitmap.BackBuffer;
            var length = writeableBitmap.PixelWidth * writeableBitmap.PixelHeight *
                writeableBitmap.Format.BitsPerPixel / 8;
            for (int i = 0, j = 0; i + 4 < length && j + 3 < list.Count; i = i + 4, j += 3)
            {
                var blue = backBuffer[i];
                var green = backBuffer[i + 1];
                var red = backBuffer[i + 2];
                var alpha = backBuffer[i + 3];

                blue = (byte)Math.Round(list[j + 2] / 100.0 * byte.MaxValue);
                green = (byte)Math.Round(list[j + 1] / 100.0 * byte.MaxValue);
                red = (byte)Math.Round(list[j + 0] / 100.0 * byte.MaxValue);
                alpha = 0xFF;

                backBuffer[i] = blue;
                backBuffer[i + 1] = green;
                backBuffer[i + 2] = red;
                backBuffer[i + 3] = alpha;
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            writeableBitmap.Unlock();
            return writeableBitmap;
        }

        private static List<int> StringNumberToList()
        {
            var stringReader = new StringReader(NumberText.PI);
            stringReader.Read();
            stringReader.Read();

            var list = new List<int>();
            while (true)
            {
                var height = ReadNumber(stringReader);
                if (height < 0)
                {
                    break;
                }

                var low = ReadNumber(stringReader);
                if (low < 0)
                {
                    break;
                }

                var number = height * 10 + low;
                list.Add(number);
            }

            return list;
        }

        private static int ReadNumber(StringReader reader)
        {
            while (true)
            {
                var text = reader.Read();
                if (text == -1)
                {
                    return -1;
                }

                var c = (char)text;

                var n = c - '0';
                if (n is >= 0 and <= 9)
                {
                    return n;
                }
            }
        }
    }
}