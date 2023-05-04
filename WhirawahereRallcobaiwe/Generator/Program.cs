// See https://aka.ms/new-console-template for more information

using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Generator
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application application = new Application();
            application.Startup += (s, e) =>
            {

                /*
                <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                    <BitmapImage x:Key="Image1" UriSource="Assets\1.png"/>
                </ResourceDictionary>
                 */

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\r\n                                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
                for (int i = 0; i < 100; i++)
                {
                    stringBuilder.AppendLine($"<BitmapImage x:Key=\"Image{i}\" UriSource=\"Assets\\{i}.png\"/>");
                }
                stringBuilder.AppendLine("</ResourceDictionary>");
                File.WriteAllText("ResourceDictionary1.xaml", stringBuilder.ToString());

                for (int j = 0; j < 100; j++)
                {
                    // 生成一些测试使用的文件
                    WriteableBitmap writeableBitmap = new WriteableBitmap(1024, 1024, 96, 96, PixelFormats.Pbgra32, null);

                    writeableBitmap.Lock();
                    unsafe
                    {
                        var length = writeableBitmap.PixelWidth * writeableBitmap.PixelHeight *
                           writeableBitmap.Format.BitsPerPixel / 8;
                        var backBuffer = (byte*) writeableBitmap.BackBuffer;
                        for (int i = 0; i + 4 < length; i = i + 4)
                        {
                            //var blue = backBuffer[i];
                            //var green = backBuffer[i + 1];
                            //var red = backBuffer[i + 2];
                            //var alpha = backBuffer[i + 3];

                            //blue = 0;
                            Span<byte> span = new Span<byte>(backBuffer, length);
                            span = span.Slice(i, 4);
                            Random.Shared.NextBytes(span);

                            //backBuffer[i] = blue;
                            //backBuffer[i + 1] = green;
                            //backBuffer[i + 2] = red;
                            //backBuffer[i + 3] = alpha;
                        }
                    }

                    writeableBitmap.Unlock();

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));

                    var file = $"{j}.png";

                    using var fileStream = File.OpenWrite(file);
                    encoder.Save(fileStream);
                }
            };
            application.Run();
        }
    }
}