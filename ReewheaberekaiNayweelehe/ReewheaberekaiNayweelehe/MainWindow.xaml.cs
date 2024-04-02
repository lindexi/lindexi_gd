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
using Microsoft.Maui.Graphics;

using SkiaSharp;

using Point = Microsoft.Maui.Graphics.Point;

namespace ReewheaberekaiNayweelehe
{
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

            SkBitmap = new SKBitmap(skImageInfo);

            //SKSurface surface = SKSurface.Create(skImageInfo, writeableBitmap.BackBuffer);
            //SkSurface = surface;
            //SkCanvas = surface.Canvas;
            SkCanvas = new SKCanvas(SkBitmap);
            SkCanvas.Clear();
            SkCanvas.Flush();

            Source = writeableBitmap;
        }

        public void Draw(Action<SKCanvas> action)
        {
            Draw(canvas =>
            {
                action(canvas);
                return null;
            });
        }

        public void Draw(Func<SKCanvas, Int32Rect?> draw)
        {
            var writeableBitmap = _writeableBitmap;
            writeableBitmap.Lock();

            var canvas = SkCanvas;
            var dirtyRect = draw(canvas);
            canvas.Flush();

            dirtyRect ??= new Int32Rect(0, 0, PixelWidth, PixelHeight);
            var pixels = SkBitmap.GetPixels(out var length);
            var stride = 4 * PixelWidth;
            writeableBitmap.WritePixels(dirtyRect.Value, pixels, (int) PixelWidth * PixelHeight * 4, stride);
            writeableBitmap.AddDirtyRect(dirtyRect.Value);
            writeableBitmap.Unlock();
        }

        private WriteableBitmap _writeableBitmap = null!; // 这里的 null! 是 C# 的新语法，是给智能分析用的，表示这个字段在使用的时候不会为空
        //public SKSurface SkSurface { get; private set; } = null!; // 实际上 null! 的含义是我明确给他一个空值，也就是说如果是空也是预期的
        public SKBitmap SkBitmap { get; private set; } = null!; // 实际上 null! 的含义是我明确给他一个空值，也就是说如果是空也是预期的
        public SKCanvas SkCanvas { get; private set; } = null!; // 实际上 null! 的含义是我明确给他一个空值，也就是说如果是空也是预期的

        public int PixelWidth => (int) Width;
        public int PixelHeight => (int) Height;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Draw(Action<SKCanvas> action)
        {
            Image.Draw(action);
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Draw(canvas =>
            {
                //using var skPaint = new SKPaint() { Color = new SKColor(0, 0, 0), TextSize = 100 };
                //canvas.DrawLine(10, 10, 100, 100, skPaint);

                _canvas.SkBitmap = Image.SkBitmap;

                _canvas.SetCanvas(canvas);

                using var skPaint = new SKPaint();
                skPaint.StrokeWidth = 10f;
                skPaint.Color = SKColors.Red;
                skPaint.IsAntialias = true;
                skPaint.FilterQuality = SKFilterQuality.High;
                skPaint.Style = SKPaintStyle.Stroke;

                canvas.DrawCircle(300, 300, 100, skPaint);
            });
        }

        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            Draw(canvas =>
            {
                //_canvas.SkSurface = Image.SkSurface;
                _canvas.SkBitmap = Image.SkBitmap;

                _canvas.SetCanvas(canvas);
                _canvas.Move(new Point(position.X, position.Y));

                //using var skPaint = new SKPaint() { Color = new SKColor(0, 0, 0), TextSize = 100 };
                //canvas.DrawLine(new SKPoint((float) _lastPosition.X, (float) _lastPosition.Y),
                //    new SKPoint((float) position.X, (float) position.Y), skPaint);
            });

            //_lastPosition = position;
        }

        //private Point _lastPosition = new Point(0, 0);

        private readonly SkInkCanvas _canvas = new SkInkCanvas();
    }
}