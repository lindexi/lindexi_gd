using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;

using SkiaSharp;

using System;
using System.IO;

namespace YewhuhuciDacayfudawi;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
        RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        var testFileName = "Test.png";
        testFileName = Path.GetFullPath(testFileName);

        if (!File.Exists(testFileName))
        {
            var width = 4000;
            var height = 2000;
            using var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var skCanvas = new SKCanvas(skBitmap))
            {
                using var skPaint = new SKPaint();
                skPaint.IsAntialias = true;
                skPaint.Shader = SKShader.CreateRadialGradient(new SKPoint(width / 2f, height / 2f),
                    Math.Max(width, height),
                    new SKColor[]
                    {
                        SKColors.White,
                        SKColors.Blue,
                        SKColors.LightBlue,
                        SKColors.CornflowerBlue,
                        SKColors.LightSkyBlue,
                    }, SKShaderTileMode.Clamp);
                skPaint.Style = SKPaintStyle.Fill;

                skCanvas.DrawRect(0, 0, width, height, skPaint);
            }

            using var fileStream = File.Create(testFileName);
            skBitmap.Encode(fileStream, SKEncodedImageFormat.Png, 100);
        }

        TheImage.Source = new Bitmap(testFileName);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _isDown = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_isDown)
        {
            var currentPoint = e.GetCurrentPoint(this);

            var positionX = currentPoint.Position.X;

            var translateTransform = (TranslateTransform) TheImage.RenderTransform!;
            translateTransform.X = positionX;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isDown = false;
    }

    private bool _isDown;

    private void IncreaseSizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _relativeSize += 100;
        UpdateImageSize();
    }

    private void DecreaseSizeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _relativeSize -= 100;
        UpdateImageSize();
    }

    private double _relativeSize;

    private void RootGrid_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateImageSize();
    }

    private void UpdateImageSize()
    {
        var (width, height) = RootGrid.Bounds.Size;
        TheImage.Width = width + _relativeSize;
        TheImage.Height = height + _relativeSize;
    }
}