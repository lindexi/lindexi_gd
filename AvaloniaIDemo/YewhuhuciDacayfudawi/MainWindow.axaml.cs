using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace YewhuhuciDacayfudawi;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
        RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
    }

    private List<string>? _fileList;

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        var testFileName = "Test_1K.png";
        testFileName = Path.Join(AppContext.BaseDirectory, testFileName);
        var fileList = new List<string>();

        if (!File.Exists(testFileName))
        {
            fileList.Add(GenerateTestImage(testFileName, 1024, 1024));
            fileList.Add(GenerateTestImage("Test_4K.png", 4000, 2000));
            fileList.Add(GenerateTestImage("Test_8K.png", 8000, 4000));
            _fileList = fileList;
        }
        else
        {
            fileList.AddRange(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.png"));
            _fileList = fileList;
        }

        SetImageSource("Test_8K.png");
    }

    private void SetImageSource(string testFileName)
    {
        if (_fileList is null)
        {
            throw new InvalidOperationException();
        }

        if (_fileList.FirstOrDefault(t => t.EndsWith(testFileName)) is { } filePath)
        {
            TheImage.Source = new Bitmap(filePath);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    private string GenerateTestImage(string testFileName, int width, int height)
    {
        if (!Path.IsPathFullyQualified(testFileName))
        {
            testFileName = Path.Join(AppContext.BaseDirectory, testFileName);
        }

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

        return testFileName;
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

        var x = _relativeSize;
        var y = _relativeSize;

        if (double.IsFinite(TheImage.Width) && double.IsFinite(TheImage.Height))
        {
            if (TheImage.Width > TheImage.Height)
            {
                x = TheImage.Width / TheImage.Height * y;
            }
            else
            {
                y = TheImage.Height / TheImage.Width * x;
            }
        }

        TheImage.Width = width + x;
        TheImage.Height = height + y;
    }

    private void Use1KImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SetImageSource("Test_1K.png");
    }

    private void Use4KImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SetImageSource("Test_4K.png");
    }

    private void Use8KImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SetImageSource("Test_8K.png");
    }
}