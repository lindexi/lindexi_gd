using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PptxGenerator;

/// <summary>
/// Avalonia 渲染引擎实现，包含全部 UI 框架依赖。
/// </summary>
internal sealed class AvaloniaSlideRenderEngine : IAvaloniaSlideRenderEngine
{
    private readonly Dictionary<string, TextLayout> _textLayoutCache = new();
    private readonly Dictionary<string, Bitmap?> _bitmapCache = new();

    public SlideElementMeasurements PreMeasure(SlidePage page, SlidePipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        _textLayoutCache.Clear();
        _bitmapCache.Clear();

        var results = new Dictionary<string, SlideMeasureResult>();
        PreMeasureElements(page.Children, results, context);
        return new SlideElementMeasurements(results);
    }

    public void Render(SlidePage page, DrawingContext dc, SlidePipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(dc);
        ArgumentNullException.ThrowIfNull(context);

        dc.FillRectangle(CreateBrush(page.Background, Colors.White),
            new Rect(0, 0, context.CanvasWidth, context.CanvasHeight));
        DrawElements(dc, page.Children, context);
    }

    public Bitmap RenderErrorPreview(string message, SlidePipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);

        var bitmap = new RenderTargetBitmap(new PixelSize(context.CanvasWidth, context.CanvasHeight), new Vector(96, 96));
        using (var dc = bitmap.CreateDrawingContext())
        {
            dc.FillRectangle(CreateBrush("#FFF8FAFC", Colors.White), new Rect(0, 0, context.CanvasWidth, context.CanvasHeight));
            dc.DrawRectangle(new Pen(CreateBrush("#FFFCA5A5", Colors.Red), 2), new Rect(80, 80, context.CanvasWidth - 160, context.CanvasHeight - 160));

            var typeface = new Typeface(new FontFamily("Microsoft YaHei"));
            var titleText = new FormattedText("SlideML Render Error", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 32, CreateBrush("#FFB91C1C", Colors.Red))
            {
                TextAlignment = TextAlignment.Left,
                MaxTextWidth = context.CanvasWidth - 240,
                MaxTextHeight = 80,
                LineHeight = 40,
            };

            var detailsText = new FormattedText(message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 18, CreateBrush("#FF7F1D1D", Colors.DarkRed))
            {
                TextAlignment = TextAlignment.Left,
                MaxTextWidth = context.CanvasWidth - 240,
                MaxTextHeight = context.CanvasHeight - 260,
                LineHeight = 28,
            };

            dc.DrawText(titleText, new Point(120, 120));
            dc.DrawText(detailsText, new Point(120, 190));
        }

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
        var typeface = new Typeface(new FontFamily(text.FontName));

        var maxWidth = text.Width ?? 10000;
        var maxHeight = text.Height ?? 10000;
        var lineHeight = text.FontSize * text.LineHeight;

        var textLayout = new TextLayout(
            text.Text,
            typeface,
            text.FontSize,
            foreground,
            MapTextAlignment(text.TextAlignment),
            text.Width is null ? TextWrapping.NoWrap : TextWrapping.Wrap,
            TextTrimming.None,
            null,
            FlowDirection.LeftToRight,
            maxWidth,
            maxHeight,
            lineHeight,
            0,
            0);

        _textLayoutCache[text.Id] = textLayout;

        var measuredWidth = text.Width ?? textLayout.WidthIncludingTrailingWhitespace;
        var measuredHeight = text.Height ?? textLayout.Height;

        results[text.Id] = new SlideMeasureResult
        {
            MeasuredWidth = measuredWidth,
            MeasuredHeight = measuredHeight,
            ActualLineCount = textLayout.TextLines.Count,
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

        var measuredWidth = image.Width ?? (bitmap?.PixelSize.Width ?? 240);
        var measuredHeight = image.Height ?? (bitmap?.PixelSize.Height ?? 180);

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
        if (opacity >= 1)
        {
            DrawElementCore(dc, element, context);
        }
        else
        {
            using (dc.PushOpacity(opacity))
            {
                DrawElementCore(dc, element, context);
            }
        }
    }

    private void DrawElementCore(DrawingContext dc, SlideElement element, SlidePipelineContext context)
    {
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
    }

    private void DrawPanel(DrawingContext dc, SlidePanelElement panel, SlidePipelineContext context)
    {
        var bounds = ToRect(panel.LayoutBounds);
        var backgroundBrush = CreateAvaloniaBrush(panel.Background, Colors.Transparent);
        if (backgroundBrush is not null)
        {
            dc.FillRectangle(backgroundBrush, bounds);
        }

        using (dc.PushClip(new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height)))
        {
            DrawElements(dc, panel.Children, context);
        }
    }

    private static void DrawRect(DrawingContext dc, SlideRectElement rect)
    {
        var bounds = ToRect(rect.LayoutBounds);

        var fillBrush = CreateAvaloniaBrush(rect.Fill, Colors.Transparent);
        if (fillBrush is not null)
        {
            dc.FillRectangle(fillBrush, bounds);
        }

        if (rect.StrokeThickness > 0)
        {
            var strokeBrush = CreateAvaloniaBrush(rect.Stroke, Colors.Transparent);
            if (strokeBrush is not null)
            {
                var pen = new Pen(strokeBrush, rect.StrokeThickness);
                dc.DrawRectangle(pen, bounds);
            }
        }
    }

    private void DrawText(DrawingContext dc, SlideTextElement text)
    {
        if (!_textLayoutCache.TryGetValue(text.Id, out var textLayout) || textLayout is null)
        {
            return;
        }

        var x = text.LayoutBounds.X;
        var y = text.LayoutBounds.Y;
        textLayout.Draw(dc, new Point(x, y));

        if (text.ActualLineCount == 0 && textLayout.TextLines.Count > 0)
        {
            text.ActualLineCount = textLayout.TextLines.Count;
        }
    }

    private void DrawImage(DrawingContext dc, SlideImageElement image)
    {
        var bounds = ToRect(image.LayoutBounds);

        if (_bitmapCache.TryGetValue(image.Id, out var bitmap) && bitmap is not null)
        {
            dc.DrawImage(bitmap, bounds);
            return;
        }

        dc.FillRectangle(CreateBrush("#FFF8FAFC", Colors.White), bounds);
        dc.DrawRectangle(new Pen(CreateBrush("#FFCBD5E1", Colors.Gray), 1), bounds);

        var typeface = new Typeface(new FontFamily("Microsoft YaHei"));
        var label = new FormattedText("Image", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 22, CreateBrush("#FF64748B", Colors.Gray))
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = bounds.Width,
            MaxTextHeight = 48,
        };
        dc.DrawText(label, new Point(bounds.X, bounds.Y + bounds.Height * 0.4));
    }

    private static Rect ToRect(SlideRect r) => new(r.X, r.Y, r.Width, r.Height);

    /// <summary>
    /// 将 <see cref="ISlideMlBrush"/> 转换为 Avalonia <see cref="IBrush"/>。
    /// </summary>
    private static IBrush? CreateAvaloniaBrush(ISlideMlBrush? brush, Color fallbackColor)
    {
        return brush switch
        {
            SlideMlSolidColorBrush solid => CreateBrush(solid.Color, fallbackColor),
            SlideMlLinearGradientBrush gradient => CreateGradientBrush(gradient),
            null => null,
        };
    }

    private static ISolidColorBrush CreateBrush(string colorText, Color fallbackColor)
    {
        if (!string.IsNullOrWhiteSpace(colorText))
        {
            try
            {
                return new SolidColorBrush(Color.Parse(colorText));
            }
            catch (FormatException)
            {
            }
        }

        return new SolidColorBrush(fallbackColor);
    }

    private static IBrush? CreateGradientBrush(SlideMlLinearGradientBrush gradient)
    {
        if (gradient.Stops.Count == 0)
        {
            return null;
        }

        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(gradient.X1, gradient.Y1, RelativeUnit.Relative),
            EndPoint = new RelativePoint(gradient.X2, gradient.Y2, RelativeUnit.Relative),
        };

        foreach (var stop in gradient.Stops)
        {
            brush.GradientStops.Add(new GradientStop(Color.Parse(stop.Color), stop.Offset));
        }

        return brush;
    }

    private static Bitmap? TryLoadBitmap(string source)
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
                return new Bitmap(path);
            }
            catch (IOException)
            {
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidateImagePaths(string source)
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

    private static TextAlignment MapTextAlignment(SlideTextAlignment textAlignment)
    {
        return textAlignment switch
        {
            SlideTextAlignment.Center => TextAlignment.Center,
            SlideTextAlignment.Right => TextAlignment.Right,
            SlideTextAlignment.Justify => TextAlignment.Justify,
            _ => TextAlignment.Left,
        };
    }
}