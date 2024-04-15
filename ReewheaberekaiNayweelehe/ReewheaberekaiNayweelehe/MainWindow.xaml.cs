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
using System.Xml;
using Microsoft.Maui.Graphics;

using SkiaSharp;

using StylusPoint = BujeeberehemnaNurgacolarje.StylusPoint;

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

            Draw(canvas =>
            {
                var eraserView = new EraserView();
                using var skBitmap = eraserView.GetEraserView(30, 45);
                canvas.DrawBitmap(skBitmap, 100, 100);
            });
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

        public void Update()
        {
            var writeableBitmap = _writeableBitmap;
            writeableBitmap.Lock();

            //var dirtyRect = new Int32Rect((int)rect.X, (int)rect.Y, (int) rect.Width, (int) rect.Height);
            //dirtyRect = new Int32Rect(100, 100, 600, 600);
            // 由于 Skia 不支持范围读取，因此这里需要全部刷新
            var dirtyRect = new Int32Rect(0, 0, PixelWidth, PixelHeight);

            var pixels = SkBitmap.GetPixels(out var length);
            var stride = 4 * PixelWidth;
            writeableBitmap.WritePixels(dirtyRect, pixels, (int) PixelWidth * PixelHeight * 4, stride);
            writeableBitmap.AddDirtyRect(dirtyRect);
            writeableBitmap.Unlock();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TouchDown += MainWindow_TouchDown;
            TouchMove += MainWindow_TouchMove;
            TouchUp += MainWindow_TouchUp;

            TouchLeave += MainWindow_TouchLeave;

            _canvas.RenderBoundsChanged += (o, rect) =>
            {
                Image.Update();
            };

            //_canvas.EnterEraserMode();

            Background = Brushes.Black;
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            _canvas.ApplicationDrawingSkBitmap = Image.SkBitmap;

            _canvas.SetCanvas(Image.SkCanvas);

            var touchPoint = e.GetTouchPoint(this);
            _canvas.Down(new InkingInputInfo(e.TouchDevice.Id, new StylusPoint(touchPoint.Position.X, touchPoint.Position.Y), (ulong) e.Timestamp));

            _canvas.Color = new SKColor((uint) Random.Shared.Next() | unchecked((uint) (0xFF << 24)));
        }

        private void MainWindow_TouchMove(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            _canvas.Move(new InkingInputInfo(e.TouchDevice.Id, new StylusPoint(touchPoint.Position.X, touchPoint.Position.Y), (ulong) e.Timestamp));
        }

        private void MainWindow_TouchUp(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            _canvas.Up(new InkingInputInfo(e.TouchDevice.Id, new StylusPoint(touchPoint.Position.X, touchPoint.Position.Y), (ulong) e.Timestamp));
        }

        private void MainWindow_TouchLeave(object sender, TouchEventArgs e)
        {
            var touchAction = e.GetTouchPoint(this).Action;
            if (touchAction == TouchAction.Up)
            {
                // 由于抬起的原因，那就不能算进离开窗口
                return;
            }

            _canvas.Leave();
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

                _canvas.ApplicationDrawingSkBitmap = Image.SkBitmap;

                _canvas.SetCanvas(canvas);

                using var skPaint = new SKPaint();
                skPaint.StrokeWidth = 10f;
                skPaint.Color = SKColors.Red;
                skPaint.IsAntialias = true;
                skPaint.FilterQuality = SKFilterQuality.High;


                //var background = GetCopy(Image.SkBitmap);
                //var skImage = SKImage.FromBitmap(background);
                //skPaint.ImageFilter = SKImageFilter.CreateImage(skImage);

                canvas.DrawRect(10, 10, 1000, 1000, skPaint);

                using var skPath = new SKPath();
                skPath.AddCircle(100, 100, 100);

                skPaint.Color = SKColors.White;

                canvas.Save();
                canvas.ClipPath(skPath, antialias: true);
                canvas.DrawRect(10, 10, 1000, 1000, skPaint);
                canvas.Restore();

                //skPaint.Color = SKColors.White;
                //canvas.DrawOval(new SKRect(100, 100, 150, 150), skPaint);

                //canvas.ClipRect(new SKRect(100, 100, 150, 150));

                //skPaint.IsAntialias = false;
                //canvas.DrawCircle(100, 100, 100, skPaint);
                // 以下代码可以裁剪给定矩形范围，相当于 SKCanvas 的 Clear 矩形范围的功能
                //Image.SkBitmap.Erase(SKColors.Transparent, new SKRectI(100, 100, 200, 200));


                //canvas.ClipRect(new SKRect(100,100,200,200));
            });

            SKBitmap GetCopy(SKBitmap skBitmap)
            {
                var originBackground = new SKBitmap(new SKImageInfo(skBitmap.Width, skBitmap.Height, skBitmap.ColorType, skBitmap.AlphaType,
                    skBitmap.ColorSpace), SKBitmapAllocFlags.None);

                using var skCanvas = new SKCanvas(originBackground);
                skCanvas.Clear();
                skCanvas.DrawBitmap(skBitmap, 0, 0);
                return originBackground;
            }
        }

        private readonly SkInkCanvas _canvas = new SkInkCanvas();

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }


}