using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PptxGenerator;

/// <summary>
/// 渲染引擎 WPF 实现，包含全部 UI 框架依赖。
/// 内部维护 FormattedText 和 Bitmap 缓存，避免污染数据模型。
/// </summary>
internal sealed class SlideRenderEngine : ISlideRenderEngine
{
    private readonly Dictionary<string, FormattedText> _formattedTextCache = new();
    private readonly Dictionary<string, BitmapSource?> _bitmapCache = new();

    /// <inheritdoc />
    public SlideElementMeasurements PreMeasure(SlidePage page, SlidePipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        _formattedTextCache.Clear();
        _bitmapCache.Clear();

        var results = new Dictionary<string, SlideMeasureResult>();
        PreMeasureElements(page.Children, results, context);
        return new SlideElementMeasurements(results);
    }

    /// <inheritdoc />
    public void Render(SlidePage page, DrawingContext dc, SlidePipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(dc);
        ArgumentNullException.ThrowIfNull(context);

        dc.DrawRectangle(CreateBrush(page.Background, Colors.White), null,
            new Rect(0, 0, context.CanvasWidth, context.CanvasHeight));
        DrawElements(dc, page.Children, context);
    }

    /// <inheritdoc />
    public RenderTargetBitmap RenderErrorPreview(string message, SlidePipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);

        var bitmap = new RenderTargetBitmap(context.CanvasWidth, context.CanvasHeight, 96.0, 96.0, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(CreateBrush("#FFF8FAFC", Colors.White), null, new Rect(0, 0, context.CanvasWidth, context.CanvasHeight));
            dc.DrawRectangle(null, new Pen(CreateBrush("#FFFCA5A5", Colors.Red), 2), new Rect(80, 80, context.CanvasWidth - 160, context.CanvasHeight - 160));

            var titleTypeface = new Typeface(new FontFamily("Microsoft YaHei"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var titleText = new FormattedText(
                "SlideML Render Error",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                titleTypeface,
                32,
                CreateBrush("#FFB91C1C", Colors.Red),
                GetDpi())
            {
                TextAlignment = TextAlignment.Left,
                MaxTextWidth = context.CanvasWidth - 240,
                MaxTextHeight = 80,
                LineHeight = 40,
                Trimming = TextTrimming.None,
            };

            var detailsTypeface = new Typeface(new FontFamily("Microsoft YaHei"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var detailsText = new FormattedText(
                message,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                detailsTypeface,
                18,
                CreateBrush("#FF7F1D1D", Colors.DarkRed),
                GetDpi())
            {
                TextAlignment = TextAlignment.Left,
                MaxTextWidth = context.CanvasWidth - 240,
                MaxTextHeight = context.CanvasHeight - 260,
                LineHeight = 28,
                Trimming = TextTrimming.CharacterEllipsis,
            };

            dc.DrawText(titleText, new Point(120, 120));
            dc.DrawText(detailsText, new Point(120, 190));
        }
        bitmap.Render(visual);
        return bitmap;
    }

    private void PreMeasureElements(IReadOnlyList<SlideElement> elements, Dictionary<string, SlideMeasureResult> results, SlidePipelineContext context)
    {
        foreach (var element in elements)
        {
            PreMeasureElement(element, results, context);
        }
    }

    private void PreMeasureElement(SlideElement element, Dictionary<string, SlideMeasureResult> results, SlidePipelineContext context)
    {
        switch (element)
        {
            case SlidePanelElement panel:
                PreMeasureElements(panel.Children, results, context);
                break;
            case SlideTextElement text:
                PreMeasureText(text, results);
                break;
            case SlideImageElement image:
                PreMeasureImage(image, results, context);
                break;
        }
    }

    private void PreMeasureText(SlideTextElement text, Dictionary<string, SlideMeasureResult> results)
    {
        var foreground = CreateBrush(text.Foreground, Colors.Black);
        var typeface = new Typeface(new FontFamily(text.FontName), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        var maxWidth = text.Width ?? 10000;
        var maxHeight = text.Height ?? 10000;
        var lineHeight = text.FontSize * text.LineHeight;

        var formattedText = new FormattedText(
            text.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            text.FontSize,
            foreground,
            GetDpi());

        formattedText.TextAlignment = MapTextAlignment(text.TextAlignment);
        formattedText.MaxTextWidth = text.Width is null ? 0 : maxWidth;
        formattedText.MaxTextHeight = maxHeight;
        formattedText.LineHeight = lineHeight;

        var measuredWidth = text.Width ?? formattedText.WidthIncludingTrailingWhitespace;
        var measuredHeight = text.Height ?? formattedText.Height;
        var actualLineCount = (int)Math.Ceiling(formattedText.Height / lineHeight);
        if (actualLineCount == 0 && !string.IsNullOrEmpty(text.Text))
        {
            actualLineCount = 1;
        }

        _formattedTextCache[text.Id] = formattedText;

        results[text.Id] = new SlideMeasureResult
        {
            MeasuredWidth = measuredWidth,
            MeasuredHeight = measuredHeight,
            ActualLineCount = actualLineCount,
        };
    }

    private void PreMeasureImage(SlideImageElement image, Dictionary<string, SlideMeasureResult> results, SlidePipelineContext context)
    {
        var bitmap = TryLoadBitmap(image.Source);
        _bitmapCache[image.Id] = bitmap;

        if (bitmap is null)
        {
            context.AddWarning($"[Warning] {image.Id}: 图片资源 {image.Source} 未找到，已使用占位图");
        }

        var measuredWidth = image.Width ?? (bitmap?.PixelWidth ?? 240);
        var measuredHeight = image.Height ?? (bitmap?.PixelHeight ?? 180);

        results[image.Id] = new SlideMeasureResult
        {
            MeasuredWidth = measuredWidth,
            MeasuredHeight = measuredHeight,
        };
    }

    private void DrawElements(DrawingContext dc, IReadOnlyList<SlideElement> elements, SlidePipelineContext context)
    {
        foreach (var element in elements)
        {
            DrawElement(dc, element, context);
        }
    }

    private void DrawElement(DrawingContext dc, SlideElement element, SlidePipelineContext context)
    {
        var opacity = Math.Clamp(element.Opacity, 0, 1);
        dc.PushOpacity(opacity);

        switch (element)
        {
            case SlidePanelElement panel:
                DrawPanel(dc, panel, context);
                break;
            case SlideRectElement rect:
                DrawRect(dc, rect);
                break;
            case SlideTextElement text:
                DrawText(dc, text);
                break;
            case SlideImageElement image:
                DrawImage(dc, image);
                break;
        }

        dc.Pop();
    }

    private void DrawPanel(DrawingContext dc, SlidePanelElement panel, SlidePipelineContext context)
    {
        if (!string.IsNullOrWhiteSpace(panel.Background))
        {
            dc.DrawRectangle(CreateBrush(panel.Background, Colors.Transparent), null, panel.LayoutBounds);
        }

        dc.PushClip(new RectangleGeometry(panel.LayoutBounds));
        DrawElements(dc, panel.Children, context);
        dc.Pop();
    }

    private static void DrawRect(DrawingContext dc, SlideRectElement rect)
    {
        var fill = string.IsNullOrWhiteSpace(rect.Fill) ? null : CreateBrush(rect.Fill, Colors.Transparent);
        var pen = string.IsNullOrWhiteSpace(rect.Stroke) || rect.StrokeThickness <= 0
            ? null
            : new Pen(CreateBrush(rect.Stroke, Colors.Transparent), rect.StrokeThickness);

        if (rect.CornerRadius > 0)
        {
            dc.DrawRoundedRectangle(fill, pen, rect.LayoutBounds, rect.CornerRadius, rect.CornerRadius);
        }
        else
        {
            dc.DrawRectangle(fill, pen, rect.LayoutBounds);
        }
    }

    private void DrawText(DrawingContext dc, SlideTextElement text)
    {
        if (!_formattedTextCache.TryGetValue(text.Id, out var formattedText) || formattedText is null)
        {
            return;
        }

        if (text.Height is double fixedHeight)
        {
            dc.PushClip(new RectangleGeometry(new Rect(text.LayoutBounds.X, text.LayoutBounds.Y, text.LayoutBounds.Width, fixedHeight)));
            dc.DrawText(formattedText, text.LayoutBounds.TopLeft);
            dc.Pop();
        }
        else
        {
            dc.DrawText(formattedText, text.LayoutBounds.TopLeft);
        }
    }

    private void DrawImage(DrawingContext dc, SlideImageElement image)
    {
        var bounds = image.LayoutBounds;
        if (_bitmapCache.TryGetValue(image.Id, out var bitmap) && bitmap is not null)
        {
            var sourceRect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
            var destinationRect = CalculateImageDestination(bounds, sourceRect, image.Stretch);
            dc.DrawImage(bitmap, destinationRect);
            return;
        }

        dc.DrawRectangle(
            CreateBrush("#FFF8FAFC", Colors.White),
            new Pen(CreateBrush("#FFCBD5E1", Colors.Gray), 1),
            new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height));

        var titleTypeface = new Typeface(new FontFamily("Microsoft YaHei"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var titleText = new FormattedText(
            "Image",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            titleTypeface,
            22,
            CreateBrush("#FF64748B", Colors.Gray),
            GetDpi())
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = bounds.Width,
            MaxTextHeight = 48,
            LineHeight = 28,
        };

        var sourceTypeface = new Typeface(new FontFamily("Microsoft YaHei"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var sourceText = new FormattedText(
            image.Source,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            sourceTypeface,
            14,
            CreateBrush("#FF94A3B8", Colors.Gray),
            GetDpi())
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = Math.Max(0, bounds.Width - 32),
            MaxTextHeight = Math.Max(0, bounds.Height - 80),
            LineHeight = 18,
            Trimming = TextTrimming.CharacterEllipsis,
        };

        dc.DrawText(titleText, new Point(bounds.X, bounds.Y + Math.Max(16, bounds.Height * 0.32)));
        dc.DrawText(sourceText, new Point(bounds.X + 16, bounds.Y + Math.Max(48, bounds.Height * 0.32 + 36)));
    }

    internal static Rect CalculateImageDestination(Rect destinationBounds, Rect sourceBounds, SlideImageStretch stretch)
    {
        if (stretch == SlideImageStretch.Fill || sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
        {
            return destinationBounds;
        }

        if (stretch == SlideImageStretch.None)
        {
            return new Rect(destinationBounds.X, destinationBounds.Y, Math.Min(sourceBounds.Width, destinationBounds.Width), Math.Min(sourceBounds.Height, destinationBounds.Height));
        }

        var scaleX = destinationBounds.Width / sourceBounds.Width;
        var scaleY = destinationBounds.Height / sourceBounds.Height;
        var scale = stretch == SlideImageStretch.Uniform ? Math.Min(scaleX, scaleY) : Math.Max(scaleX, scaleY);
        var width = sourceBounds.Width * scale;
        var height = sourceBounds.Height * scale;
        var x = destinationBounds.X + (destinationBounds.Width - width) / 2;
        var y = destinationBounds.Y + (destinationBounds.Height - height) / 2;
        return new Rect(x, y, width, height);
    }

    internal static TextAlignment MapTextAlignment(SlideTextAlignment textAlignment)
    {
        return textAlignment switch
        {
            SlideTextAlignment.Center => TextAlignment.Center,
            SlideTextAlignment.Right => TextAlignment.Right,
            SlideTextAlignment.Justify => TextAlignment.Justify,
            _ => TextAlignment.Left,
        };
    }

    internal static SolidColorBrush CreateBrush(string colorText, Color fallbackColor)
    {
        if (!string.IsNullOrWhiteSpace(colorText))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorText);
                return new SolidColorBrush(color);
            }
            catch (FormatException)
            {
            }
        }

        return new SolidColorBrush(fallbackColor);
    }

    internal static BitmapSource? TryLoadBitmap(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        foreach (var path in GetCandidateImagePaths(source))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                return BitmapFrame.Create(new Uri(path, UriKind.Absolute), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch (IOException)
            {
            }
        }

        return null;
    }

    internal static IEnumerable<string> GetCandidateImagePaths(string source)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        yield return source;
        yield return Path.Join(currentDirectory, source);

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" })
        {
            yield return Path.Join(currentDirectory, source + extension);
            yield return Path.Join(currentDirectory, "Images", source + extension);
            yield return Path.Join(currentDirectory, "Assets", source + extension);
        }
    }

    private static double GetDpi()
    {
        return VisualTreeHelper.GetDpi(Application.Current.MainWindow ?? Application.Current.Windows.OfType<Window>().FirstOrDefault()).PixelsPerDip;
    }
}
