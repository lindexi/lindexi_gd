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

    public class SkiaCanvas : Image
    {
        public SkiaCanvas()
        {
            Loaded += SkiaCanvas_Loaded;
        }

        private void SkiaCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            var writeableBitmap = new WriteableBitmap(PixelWidth, PixelHeight, 96, 96, PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent);

            _writeableBitmap = writeableBitmap;

            var skImageInfo = new SKImageInfo()
            {
                Width = PixelWidth,
                Height = PixelHeight,
                ColorType = SKColorType.Bgra8888,
                AlphaType = SKAlphaType.Premul,
                ColorSpace = SKColorSpace.CreateSrgb()
            };

            SKSurface surface = SKSurface.Create(skImageInfo, writeableBitmap.BackBuffer);
            _skSurface = surface;

            Source = writeableBitmap;
        }

        private WriteableBitmap _writeableBitmap = null!; // 这里的 null! 是 C# 的新语法，是给智能分析用的，表示这个字段在使用的时候不会为空
        private SKSurface _skSurface = null!; // 实际上 null! 的含义是我明确给他一个空值，也就是说如果是空也是预期的

        public int PixelWidth => (int) Width;
        public int PixelHeight => (int) Height;
    }
}
