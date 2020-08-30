using System;
using System.Collections.Generic;
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
using SkiaSharp;

namespace ReewheaberekaiNayweelehe
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
            var writeableBitmap = CreateImage(1920, 1080);
            UpdateImage(writeableBitmap);
            Image.Source = writeableBitmap;
        }

        private WriteableBitmap CreateImage(int width, int height)
        {
            var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
            return writeableBitmap;
        }

        private void UpdateImage(WriteableBitmap writeableBitmap)
        {
            int width = (int)writeableBitmap.Width,
                height = (int)writeableBitmap.Height;
            writeableBitmap.Lock();
            var skImageInfo = new SKImageInfo()
            {
                Width = width,
                Height = height,
                ColorType = SKColorType.Bgra8888,
                AlphaType = SKAlphaType.Premul,
                ColorSpace = SKColorSpace.CreateSrgb()
            };

            var name = "微软雅黑";
            var skTypeface = SKTypeface.FromFamilyName(name);
            if (skTypeface.FamilyName != name)
            {
                // 字体加载失败了
                skTypeface.Dispose();
            }

            var fontFamily = new FontFamily(name);
            foreach (var familyNamesValue in fontFamily.FamilyNames.Values)
            {
                skTypeface = SKTypeface.FromFamilyName(familyNamesValue);
                if (skTypeface.FamilyName == familyNamesValue)
                {
                    break;
                }
                else
                {
                    skTypeface.Dispose();
                }
            }
          
            using (var surface = SKSurface.Create(skImageInfo, writeableBitmap.BackBuffer))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(new SKColor(130, 130, 130));
                using var skPaint = new SKPaint() { Color = new SKColor(0, 0, 0), TextSize = 100 };
                canvas.DrawText("SkiaSharp on Wpf!", 50, 200, skPaint);
                using var paint = new SKPaint(new SKFont(skTypeface))
                {
                    Color = new SKColor(0, 0, 0),
                    TextSize = 20
                };
                canvas.DrawText("星系", new SKPoint(50, 500), paint);
            }

            skTypeface.Dispose();

            Task.Run(() =>
            {
              
            }).ConfigureAwait(false).GetAwaiter().GetResult();
            
            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            writeableBitmap.Unlock();
        }
    }
}
