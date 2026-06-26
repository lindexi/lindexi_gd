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
using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator;

/// <summary>
/// Avalonia 渲染引擎实现，包含全部 UI 框架依赖。
/// </summary>
internal sealed class AvaloniaSlideRenderEngine : ISlideMlRenderEngine
{
    private readonly Dictionary<string, TextLayout> _textLayoutCache = new();
    private readonly Dictionary<string, List<TextLayout>> _spanTextLayoutCache = new();
    private readonly Dictionary<string, Bitmap?> _bitmapCache = new();

    public SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        _textLayoutCache.Clear();
        _spanTextLayoutCache.Clear();
        _bitmapCache.Clear();

        var results = new Dictionary<string, SlideMlMeasureResult>();
        PreMeasureElements(page.Children, results, context);
        return new SlideMlElementMeasurements(results);
    }

    public IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        var bitmap = new RenderTargetBitmap(new PixelSize(context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight), new Vector(96, 96));
        using (var dc = bitmap.CreateDrawingContext())
        {
            dc.FillRectangle(CreateBrush(page.Background, Colors.White),
                new Rect(0, 0, context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight));
            DrawElements(dc, page.Children, context);
        }
        return new AvaloniaPreviewImage(bitmap);
    }

    public IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);

        var bitmap = new RenderTargetBitmap(new PixelSize(context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight), new Vector(96, 96));
        using (var dc = bitmap.CreateDrawingContext())
        {
            dc.FillRectangle(CreateBrush("#FFF8FAFC", Colors.White), new Rect(0, 0, context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight));
            dc.DrawRectangle(new Pen(CreateBrush("#FFFCA5A5", Colors.Red), 2), new Rect(80, 80, context.SlideDocumentContext.CanvasWidth - 160, context.SlideDocumentContext.CanvasHeight - 160));

            var typeface = new Typeface(new FontFamily("Microsoft YaHei"));
            var titleText = new FormattedText("SlideML Render Error", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 32, CreateBrush("#FFB91C1C", Colors.Red))
            {
                TextAlignment = TextAlignment.Left,
                MaxTextWidth = context.SlideDocumentContext.CanvasWidth - 240,
                MaxTextHeight = 80,
                LineHeight = 40,
            };

            var detailsText = new FormattedText(message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 18, CreateBrush("#FF7F1D1D", Colors.DarkRed))
            {
                TextAlignment = TextAlignment.Left,
                MaxTextWidth = context.SlideDocumentContext.CanvasWidth - 240,
                MaxTextHeight = context.SlideDocumentContext.CanvasHeight - 260,
                LineHeight = 28,
            };

            dc.DrawText(titleText, new Point(120, 120));
            dc.DrawText(detailsText, new Point(120, 190));
        }

        return new AvaloniaPreviewImage(bitmap);
    }

    private void PreMeasureElements(IReadOnlyList<SlideMlElement> elements, Dictionary<string, SlideMlMeasureResult> results, SlideMlPipelineContext context)
    {
        foreach (var element in elements)
        {
            PreMeasureElement(element, results, context);
        }
    }

    private void PreMeasureElement(SlideMlElement element, Dictionary<string, SlideMlMeasureResult> results, SlideMlPipelineContext context)
    {
        switch (element)
        {
            case SlideMlPanelElement panel:
                PreMeasureElements(panel.Children, results, context);
                break;
            case SlideMlTextElement text:
                PreMeasureText(text, results);
                break;
            case SlideMlImageElement image:
                PreMeasureImage(image, results, context);
                break;
        }
    }

    private void PreMeasureText(SlideMlTextElement text, Dictionary<string, SlideMlMeasureResult> results)
    {
        var maxWidth = text.Width ?? 10000;
        var maxHeight = text.Height ?? 10000;
        var lineHeight = text.FontSize;

        if (text.Spans is { Count: > 0 })
        {
            PreMeasureSpans(text, results, maxWidth, maxHeight, lineHeight);
            return;
        }

        var foreground = CreateBrush(text.Foreground, Colors.Black);
        var fontWeight = text.IsBold == true ? FontWeight.Bold : FontWeight.Normal;
        var fontStyle = text.IsItalic == true ? FontStyle.Italic : FontStyle.Normal;
        var typeface = new Typeface(new FontFamily(text.FontName), fontStyle, fontWeight);

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

        results[text.Id] = new SlideMlMeasureResult
        {
            MeasuredWidth = measuredWidth,
            MeasuredHeight = measuredHeight,
            ActualLineCount = textLayout.TextLines.Count,
        };
    }

    /// <summary>
    /// 预测量富文本 Span，逐段创建 TextLayout 并拼接。
    /// </summary>
    private void PreMeasureSpans(SlideMlTextElement text, Dictionary<string, SlideMlMeasureResult> results,
        double maxWidth, double maxHeight, double lineHeight)
    {
        var spans = text.Spans!;
        var layouts = new List<TextLayout>(spans.Count);

        double totalWidth = 0;
        double totalHeight = 0;

        foreach (var span in spans)
        {
            var spanFontWeight = (span.IsBold ?? text.IsBold) == true ? FontWeight.Bold : FontWeight.Normal;
            var spanFontStyle = (span.IsItalic ?? text.IsItalic) == true ? FontStyle.Italic : FontStyle.Normal;
            var spanFontName = span.FontName ?? text.FontName;
            var spanFontSize = span.FontSize ?? text.FontSize;
            var spanForeground = CreateBrush(span.Foreground ?? text.Foreground, Colors.Black);
            var spanTypeface = new Typeface(new FontFamily(spanFontName), spanFontStyle, spanFontWeight);

            var spanLayout = new TextLayout(
                span.Text,
                spanTypeface,
                spanFontSize,
                spanForeground,
                MapTextAlignment(text.TextAlignment),
                text.Width is null ? TextWrapping.NoWrap : TextWrapping.Wrap,
                TextTrimming.None,
                null,
                FlowDirection.LeftToRight,
                Math.Max(0, maxWidth - totalWidth),
                maxHeight,
                spanFontSize,
                0,
                0);

            layouts.Add(spanLayout);
            totalWidth += spanLayout.WidthIncludingTrailingWhitespace;
            totalHeight = Math.Max(totalHeight, spanLayout.Height);
        }

        _spanTextLayoutCache[text.Id] = layouts;

        var measuredWidth = text.Width ?? totalWidth;
        var measuredHeight = text.Height ?? totalHeight;
        var actualLineCount = totalHeight > 0 ? (int)Math.Ceiling(totalHeight / lineHeight) : 0;
        if (actualLineCount == 0 && spans.Count > 0)
        {
            actualLineCount = 1;
        }

        results[text.Id] = new SlideMlMeasureResult
        {
            MeasuredWidth = measuredWidth,
            MeasuredHeight = measuredHeight,
            ActualLineCount = actualLineCount,
        };
    }

    private void PreMeasureImage(SlideMlImageElement image, Dictionary<string, SlideMlMeasureResult> results, SlideMlPipelineContext context)
    {
        var bitmap = TryLoadBitmap(image.Source);
        _bitmapCache[image.Id] = bitmap;

        if (bitmap is null)
        {
            context.AddWarning($"[Warning] {image.Id}: 图片资源 {image.Source} 未找到，已使用占位图");
        }

        var measuredWidth = image.Width ?? (bitmap?.PixelSize.Width ?? 240);
        var measuredHeight = image.Height ?? (bitmap?.PixelSize.Height ?? 180);

        results[image.Id] = new SlideMlMeasureResult
        {
            MeasuredWidth = measuredWidth,
            MeasuredHeight = measuredHeight,
        };
    }

    private void DrawElements(DrawingContext dc, IReadOnlyList<SlideMlElement> elements, SlideMlPipelineContext context)
    {
        foreach (var element in elements)
        {
            DrawElement(dc, element, context);
        }
    }

    private void DrawElement(DrawingContext dc, SlideMlElement element, SlideMlPipelineContext context)
    {
        // 先绘制阴影（阴影不受元素 Opacity 影响）
        if (element is SlideMlRectElement rect && rect.Shadow is not null)
        {
            DrawShadow(dc, rect.Shadow, ToRect(rect.LayoutBounds), rect.CornerRadius);
        }

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

    private void DrawElementCore(DrawingContext dc, SlideMlElement element, SlideMlPipelineContext context)
    {
        switch (element)
        {
            case SlideMlPanelElement panel:
                DrawPanel(dc, panel, context);
                break;
            case SlideMlRectElement rect:
                DrawRect(dc, rect);
                break;
            case SlideMlTextElement text:
                DrawText(dc, text);
                break;
            case SlideMlImageElement image:
                DrawImage(dc, image);
                break;
        }
    }

    private void DrawPanel(DrawingContext dc, SlideMlPanelElement panel, SlideMlPipelineContext context)
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

    private static void DrawRect(DrawingContext dc, SlideMlRectElement rect)
    {
        var bounds = ToRect(rect.LayoutBounds);
        var cornerRadius = rect.CornerRadius?.TopLeft ?? 0;

        var fillBrush = CreateAvaloniaBrush(rect.Fill, Colors.Transparent);
        var strokeBrush = rect.StrokeThickness > 0
            ? CreateAvaloniaBrush(rect.Stroke, Colors.Transparent)
            : null;

        Pen? pen = null;
        if (strokeBrush is not null)
        {
            DashStyle? dashStyle = rect.StrokeDashArray is { Count: > 0 }
                ? new DashStyle(rect.StrokeDashArray, 0)
                : null;
            pen = new Pen(strokeBrush, rect.StrokeThickness, dashStyle);
        }

        if (cornerRadius > 0)
        {
            var roundedRect = new RoundedRect(bounds, cornerRadius, cornerRadius, cornerRadius, cornerRadius);
            dc.DrawRectangle(fillBrush, pen, roundedRect);
        }
        else
        {
            dc.DrawRectangle(fillBrush, pen, bounds);
        }
    }

    /// <summary>
    /// 绘制元素阴影。在元素下方绘制偏移+模糊的半透明矩形。
    /// </summary>
    private static void DrawShadow(DrawingContext dc, SlideMlShadow shadow, Rect bounds, SlideMlCornerRadius? radius)
    {
        if (shadow.Blur <= 0 && shadow.OffsetX == 0 && shadow.OffsetY == 0)
        {
            return;
        }

        var shadowBounds = new Rect(
            bounds.X + shadow.OffsetX,
            bounds.Y + shadow.OffsetY,
            bounds.Width,
            bounds.Height);

        if (shadowBounds.Width <= 0 || shadowBounds.Height <= 0)
        {
            return;
        }

        var shadowColor = CreateColor(shadow.Color, Colors.Black);
        shadowColor = new Color(
            (byte)(shadowColor.A * Math.Clamp(shadow.Opacity, 0, 1)),
            shadowColor.R,
            shadowColor.G,
            shadowColor.B);
        var shadowBrush = new SolidColorBrush(shadowColor);
        var cornerRadius = radius?.TopLeft ?? 0;

        // 模糊半径 > 0 时，通过缩放渲染模拟模糊效果
        if (shadow.Blur > 0)
        {
            DrawBlurredShadow(dc, shadowBrush, shadowBounds, cornerRadius, shadow.Blur);
        }
        else
        {
            DrawShadowRect(dc, shadowBrush, shadowBounds, cornerRadius);
        }
    }

    /// <summary>
    /// 绘制无模糊的阴影矩形。
    /// </summary>
    private static void DrawShadowRect(DrawingContext dc, IBrush shadowBrush, Rect bounds, double cornerRadius)
    {
        if (cornerRadius > 0)
        {
            dc.DrawRectangle(shadowBrush, null, new RoundedRect(bounds, cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        }
        else
        {
            dc.DrawRectangle(shadowBrush, null, bounds);
        }
    }

    /// <summary>
    /// 通过缩小渲染再放大绘制的方式模拟高斯模糊阴影。
    /// </summary>
    private static void DrawBlurredShadow(DrawingContext dc, IBrush shadowBrush, Rect bounds, double cornerRadius, double blur)
    {
        // 模糊扩展区域
        var extendedBounds = new Rect(
            bounds.X - blur,
            bounds.Y - blur,
            bounds.Width + blur * 2,
            bounds.Height + blur * 2);

        if (extendedBounds.Width <= 0 || extendedBounds.Height <= 0)
        {
            return;
        }

        // 缩小渲染到中间位图，再放大绘制实现模糊
        var scale = Math.Max(0.1, 1.0 - blur / 50.0);
        var smallWidth = Math.Max(1, (int)Math.Ceiling(extendedBounds.Width * scale));
        var smallHeight = Math.Max(1, (int)Math.Ceiling(extendedBounds.Height * scale));

        var shadowBitmap = new RenderTargetBitmap(new PixelSize(smallWidth, smallHeight), new Vector(96, 96));
        using (var sdc = shadowBitmap.CreateDrawingContext())
        {
            var smallBounds = new Rect(0, 0, smallWidth, smallHeight);
            var smallRadius = cornerRadius * scale;
            if (smallRadius > 0)
            {
                sdc.DrawRectangle(shadowBrush, null, new RoundedRect(smallBounds, smallRadius, smallRadius, smallRadius, smallRadius));
            }
            else
            {
                sdc.DrawRectangle(shadowBrush, null, smallBounds);
            }
        }

        dc.DrawImage(shadowBitmap, extendedBounds);
    }

    private void DrawText(DrawingContext dc, SlideMlTextElement text)
    {
        // 富文本 Span 渲染
        if (_spanTextLayoutCache.TryGetValue(text.Id, out var spanLayouts) && spanLayouts is { Count: > 0 })
        {
            DrawSpans(dc, text, spanLayouts);
            return;
        }

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

    /// <summary>
    /// 逐段绘制富文本 Span，每段独立样式。
    /// </summary>
    private static void DrawSpans(DrawingContext dc, SlideMlTextElement text, List<TextLayout> spanLayouts)
    {
        var x = text.LayoutBounds.X;
        var y = text.LayoutBounds.Y;

        foreach (var layout in spanLayouts)
        {
            layout.Draw(dc, new Point(x, y));
            x += layout.WidthIncludingTrailingWhitespace;
        }
    }

    private void DrawImage(DrawingContext dc, SlideMlImageElement image)
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

    private static Rect ToRect(SlideMlRect r) => new(r.X, r.Y, r.Width, r.Height);

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

    /// <summary>
    /// 从颜色字符串创建 <see cref="Color"/>，失败返回后备值。
    /// </summary>
    private static Color CreateColor(string colorText, Color fallbackColor)
    {
        if (!string.IsNullOrWhiteSpace(colorText))
        {
            try
            {
                return Color.Parse(colorText);
            }
            catch (FormatException)
            {
            }
        }

        return fallbackColor;
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

    private static TextAlignment MapTextAlignment(SlideMlTextAlignment textAlignment)
    {
        return textAlignment switch
        {
            SlideMlTextAlignment.Center => TextAlignment.Center,
            SlideMlTextAlignment.Right => TextAlignment.Right,
            SlideMlTextAlignment.Justify => TextAlignment.Justify,
            _ => TextAlignment.Left,
        };
    }
}
