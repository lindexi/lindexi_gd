using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JawjeleceeYairlubelhearrene.Models;

namespace JawjeleceeYairlubelhearrene.Services;

internal sealed class SlideImageWatermarkService
{
    public System.IO.FileInfo GetOutputImage(
        System.IO.FileInfo sourceImageFile,
        System.IO.DirectoryInfo workingDirectory,
        SlideWatermarkOptions options)
    {
        ArgumentNullException.ThrowIfNull(sourceImageFile);
        ArgumentNullException.ThrowIfNull(workingDirectory);
        ArgumentNullException.ThrowIfNull(options);

        if (!sourceImageFile.Exists)
        {
            throw new FileNotFoundException("页面截图不存在。", sourceImageFile.FullName);
        }

        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Text))
        {
            return sourceImageFile;
        }

        var cacheDirectory = new System.IO.DirectoryInfo(Path.Join(workingDirectory.FullName, "Cache", "WatermarkedSlides"));
        cacheDirectory.Create();

        var cacheKey = ComputeWatermarkCacheKey(sourceImageFile, options.Text);
        var outputImageFile = new System.IO.FileInfo(Path.Join(cacheDirectory.FullName, $"{cacheKey}.png"));
        if (outputImageFile.Exists && outputImageFile.Length > 0)
        {
            return outputImageFile;
        }

        CreateWatermarkedImage(sourceImageFile, outputImageFile, options.Text);
        return outputImageFile;
    }

    private static void CreateWatermarkedImage(System.IO.FileInfo sourceImageFile, System.IO.FileInfo targetImageFile, string watermarkText)
    {
        using var fileStream = sourceImageFile.OpenRead();
        var decoder = BitmapDecoder.Create(fileStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];

        var width = frame.PixelWidth;
        var height = frame.PixelHeight;

        var visual = new DrawingVisual();
        using (var drawingContext = visual.RenderOpen())
        {
            drawingContext.DrawImage(frame, new Rect(0, 0, width, height));
            DrawWatermark(drawingContext, visual, width, height, watermarkText);
        }

        var renderTargetBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
        using var outputStream = targetImageFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(outputStream);
    }

    private static void DrawWatermark(DrawingContext drawingContext, Visual visual, int width, int height, string watermarkText)
    {
        var dpi = VisualTreeHelper.GetDpi(visual);
        var maxFontSize = Math.Clamp(width * 0.04, 20.0, 56.0);
        var minFontSize = 14.0;
        var fontSize = maxFontSize;
        FormattedText formattedText;

        while (true)
        {
            formattedText = new FormattedText(
                watermarkText,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                fontSize,
                Brushes.White,
                dpi.PixelsPerDip);

            if (formattedText.WidthIncludingTrailingWhitespace <= width * 0.58 || fontSize <= minFontSize)
            {
                break;
            }

            fontSize -= 2;
        }

        var textGeometry = formattedText.BuildGeometry(new Point(0, 0));
        var bounds = textGeometry.Bounds;
        var margin = Math.Max(24.0, width * 0.025);
        var x = Math.Max(margin, width - bounds.Width - margin);
        var y = Math.Max(margin, height - bounds.Height - margin);

        var glowBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
        glowBrush.Freeze();
        var textBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
        textBrush.Freeze();
        var shadowBrush = new SolidColorBrush(Color.FromArgb(110, 0, 0, 0));
        shadowBrush.Freeze();

        var offsets = new[]
        {
            new Vector(-2, -2), new Vector(0, -2), new Vector(2, -2),
            new Vector(-2, 0), new Vector(2, 0),
            new Vector(-2, 2), new Vector(0, 2), new Vector(2, 2)
        };

        foreach (var offset in offsets)
        {
            var glowGeometry = textGeometry.Clone();
            glowGeometry.Transform = new TranslateTransform(x + offset.X, y + offset.Y);
            drawingContext.DrawGeometry(glowBrush, null, glowGeometry);
        }

        var shadowGeometry = textGeometry.Clone();
        shadowGeometry.Transform = new TranslateTransform(x + 1, y + 1);
        drawingContext.DrawGeometry(shadowBrush, null, shadowGeometry);

        var mainGeometry = textGeometry.Clone();
        mainGeometry.Transform = new TranslateTransform(x, y);
        drawingContext.DrawGeometry(textBrush, null, mainGeometry);
    }

    private static string ComputeWatermarkCacheKey(System.IO.FileInfo sourceImageFile, string watermarkText)
    {
        sourceImageFile.Refresh();
        var payload = string.Join(
            "|",
            "WatermarkV1",
            sourceImageFile.FullName,
            sourceImageFile.Exists ? sourceImageFile.Length : 0,
            sourceImageFile.Exists ? sourceImageFile.LastWriteTimeUtc.Ticks : 0,
            watermarkText);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
