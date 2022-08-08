using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Svg.Skia;
using Path = System.IO.Path;

namespace GafawwaybalachaCemleardearha
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var svgFile = "Test.svg";
            svgFile = Path.GetFullPath(svgFile);

            using var skSvg = new SKSvg();
            skSvg.Load(svgFile);
            if (skSvg.Picture is null)
            {
                return;
            }

            var skSvgPicture = skSvg.Picture;

            var skBitmap = skSvgPicture.ToBitmap(SKColor.Empty, 1, 1, SKColorType.Bgra8888, SKAlphaType.Premul, SKColorSpace.CreateSrgb());

            if (skBitmap is null)
            {
                return;
            }

            var writeableBitmap = new WriteableBitmap(skBitmap.Width, skBitmap.Height, 96, 96, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent);

            var skImageInfo = new SKImageInfo()
            {
                Width = skBitmap.Width,
                Height = skBitmap.Height,
                ColorType = SKColorType.Bgra8888,
                AlphaType = SKAlphaType.Premul,
                ColorSpace = SKColorSpace.CreateSrgb()
            };

            using SKSurface surface = SKSurface.Create(skImageInfo, writeableBitmap.BackBuffer);

            writeableBitmap.Lock();
            surface.Canvas.DrawBitmap(skBitmap,0,0);

            writeableBitmap.AddDirtyRect(new Int32Rect(0,0, skBitmap.Width, skBitmap.Height));
            writeableBitmap.Unlock();

            var image = new Image()
            {
                Width = 100,
                Height = 100,
                Source = writeableBitmap,
            };

            Root.Children.Add(image);
        }
    }
}
