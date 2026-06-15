using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace PptxGenerator;

/// <summary>
/// 页面渲染器，负责将页面内容渲染成其他格式，比如图片或 PPT 文档页面。
/// </summary>
public class SlideRenderer
{
    public const int CanvasWidth = 1280;
    public const int CanvasHeight = 720;

    private readonly SlideMlParser _parser = new();

    /// <summary>
    /// 将 SlideML 渲染为预览图，并返回回填后的 XML 与警告信息。
    /// </summary>
    public async Task<SlideRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slideXml))
        {
            throw new ArgumentException("SlideML 不能为空。", nameof(slideXml));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var normalizedXml = SlideXmlUtilities.NormalizeXml(SlideXmlUtilities.ExtractXml(slideXml));
            var page = _parser.Parse(normalizedXml);
            var warnings = new List<string>();

            var previewBitmap = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LayoutChildren(page.Children, page.LayoutBounds, warnings, parentId: "Page", clipToParent: false);

                var previewBitmap = new RenderTargetBitmap(new PixelSize(CanvasWidth, CanvasHeight));
                using (var context = previewBitmap.CreateDrawingContext())
                {
                    context.FillRectangle(CreateBrush(page.Background, Colors.White),
                        new Rect(0, 0, CanvasWidth, CanvasHeight));
                    DrawElements(context, page.Children, warnings);
                }

                return previewBitmap;
            });
          

            var renderedXml = SlideXmlUtilities.FormatRenderedXml(normalizedXml, id => FindMetrics(page, id));
            return new SlideRenderResult
            {
                InputXml = normalizedXml,
                OutputXml = renderedXml,
                Warnings = warnings,
                PreviewBitmap = previewBitmap,
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or System.Xml.XmlException)
        {
            var previewBitmap = await 
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    return RenderErrorPreview(ex.Message);
                });
            return new SlideRenderResult
            {
                InputXml = slideXml,
                OutputXml = slideXml,
                Warnings = [$"[Warning] parser: SlideML 解析失败，{ex.Message}"],
                PreviewBitmap = previewBitmap,
            };
        }
    }

    private static void LayoutChildren(IReadOnlyList<SlideElement> children, Rect parentBounds, List<string> warnings, string parentId, bool clipToParent)
    {
        foreach (var child in children)
        {
            LayoutElement(child, parentBounds, warnings, parentId, clipToParent);
        }
    }

    private static void LayoutElement(SlideElement element, Rect parentBounds, List<string> warnings, string parentId, bool clipToParent)
    {
        switch (element)
        {
            case SlidePanelElement panel:
                LayoutPanel(panel, parentBounds, warnings, parentId, clipToParent);
                break;
            case SlideRectElement rect:
                LayoutRect(rect, parentBounds, warnings, parentId, clipToParent);
                break;
            case SlideTextElement text:
                LayoutText(text, parentBounds, warnings, parentId, clipToParent);
                break;
            case SlideImageElement image:
                LayoutImage(image, parentBounds, warnings, parentId, clipToParent);
                break;
            default:
                throw new InvalidOperationException($"未知元素类型: {element.GetType().Name}");
        }
    }

    private static void LayoutPanel(SlidePanelElement panel, Rect parentBounds, List<string> warnings, string parentId, bool clipToParent)
    {
        var provisionalWidth = panel.Width ?? Math.Max(0, parentBounds.Width - panel.Padding * 2);
        var provisionalHeight = panel.Height ?? Math.Max(0, parentBounds.Height - panel.Padding * 2);

        var initialChildOrigin = new Point(parentBounds.X + (panel.X ?? 0) + panel.Padding, parentBounds.Y + (panel.Y ?? 0) + panel.Padding);
        var provisionalContentBounds = new Rect(initialChildOrigin.X, initialChildOrigin.Y, provisionalWidth, provisionalHeight);

        LayoutChildren(panel.Children, provisionalContentBounds, warnings, panel.Id, clipToParent: true);

        var contentRight = 0d;
        var contentBottom = 0d;
        foreach (var child in panel.Children)
        {
            contentRight = Math.Max(contentRight, child.LocalBounds.Right);
            contentBottom = Math.Max(contentBottom, child.LocalBounds.Bottom);
        }

        var actualWidth = panel.Width ?? (contentRight + panel.Padding * 2);
        var actualHeight = panel.Height ?? (contentBottom + panel.Padding * 2);

        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, actualWidth, panel.X, panel.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, actualHeight, panel.Y, panel.VerticalAlignment);

        panel.LocalBounds = new Rect(0, 0, actualWidth, actualHeight);
        panel.LayoutBounds = new Rect(originX, originY, actualWidth, actualHeight);
        panel.ActualWidth = actualWidth;
        panel.ActualHeight = actualHeight;

        var finalContentBounds = new Rect(originX + panel.Padding, originY + panel.Padding, Math.Max(0, actualWidth - panel.Padding * 2), Math.Max(0, actualHeight - panel.Padding * 2));
        LayoutChildren(panel.Children, finalContentBounds, warnings, panel.Id, clipToParent: true);

        ValidateBounds(panel, parentBounds, warnings, parentId, clipToParent);
    }

    private static void LayoutRect(SlideRectElement rect, Rect parentBounds, List<string> warnings, string parentId, bool clipToParent)
    {
        var width = rect.Width ?? 0;
        var height = rect.Height ?? 0;
        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, width, rect.X, rect.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, height, rect.Y, rect.VerticalAlignment);

        rect.LocalBounds = new Rect(rect.X ?? 0, rect.Y ?? 0, width, height);
        rect.LayoutBounds = new Rect(originX, originY, width, height);
        rect.ActualWidth = width;
        rect.ActualHeight = height;

        ValidateBounds(rect, parentBounds, warnings, parentId, clipToParent);
    }

    private static void LayoutText(SlideTextElement text, Rect parentBounds, List<string> warnings, string parentId, bool clipToParent)
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

        var measuredWidth = text.Width ?? textLayout.WidthIncludingTrailingWhitespace;
        var measuredHeight = text.Height ?? textLayout.Height;
        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, measuredWidth, text.X, text.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, measuredHeight, text.Y, text.VerticalAlignment);

        text.TextLayout = textLayout;
        text.ActualLineCount = textLayout.TextLines.Count;
        text.LocalBounds = new Rect(text.X ?? 0, text.Y ?? 0, measuredWidth, measuredHeight);
        text.LayoutBounds = new Rect(originX, originY, measuredWidth, measuredHeight);
        text.ActualWidth = measuredWidth;
        text.ActualHeight = measuredHeight;

        if (text.Height is double fixedHeight && textLayout.Height > fixedHeight + 0.1)
        {
            var averageLineHeight = textLayout.TextLines.Count == 0 ? lineHeight : textLayout.Height / textLayout.TextLines.Count;
            var visibleLineCount = averageLineHeight <= 0 ? 0 : Math.Max(0, (int) Math.Floor(fixedHeight / averageLineHeight));
            warnings.Add($"[Warning] {text.Id}: ActualLineCount={text.ActualLineCount}，超出容器高度（当前高度仅容纳 {visibleLineCount} 行）");
        }

        ValidateBounds(text, parentBounds, warnings, parentId, clipToParent);
    }

    private static void LayoutImage(SlideImageElement image, Rect parentBounds, List<string> warnings, string parentId, bool clipToParent)
    {
        var width = image.Width ?? 240;
        var height = image.Height ?? 180;
        var originX = ResolveOrigin(parentBounds.X, parentBounds.Width, width, image.X, image.HorizontalAlignment);
        var originY = ResolveOrigin(parentBounds.Y, parentBounds.Height, height, image.Y, image.VerticalAlignment);

        image.LocalBounds = new Rect(image.X ?? 0, image.Y ?? 0, width, height);
        image.LayoutBounds = new Rect(originX, originY, width, height);
        image.ActualWidth = width;
        image.ActualHeight = height;
        image.Bitmap = TryLoadBitmap(image.Source);

        if (image.Bitmap is null)
        {
            warnings.Add($"[Warning] {image.Id}: 图片资源 {image.Source} 未找到，已使用占位图");
        }

        ValidateBounds(image, parentBounds, warnings, parentId, clipToParent);
    }

    private static void ValidateBounds(SlideElement element, Rect parentBounds, List<string> warnings, string parentId, bool clipToParent)
    {
        var bounds = element.LayoutBounds;
        if (bounds.Right > CanvasWidth)
        {
            warnings.Add($"[Warning] {element.Id}: 元素右边界 X={SlideXmlUtilities.FormatNumber(bounds.Right)} 超出画布宽度 {CanvasWidth}");
        }

        if (bounds.Bottom > CanvasHeight)
        {
            warnings.Add($"[Warning] {element.Id}: 元素下边界 Y={SlideXmlUtilities.FormatNumber(bounds.Bottom)} 超出画布高度 {CanvasHeight}");
        }

        if (bounds.X < 0)
        {
            warnings.Add($"[Warning] {element.Id}: 元素左边界 X={SlideXmlUtilities.FormatNumber(bounds.X)} 超出画布左侧 0");
        }

        if (bounds.Y < 0)
        {
            warnings.Add($"[Warning] {element.Id}: 元素上边界 Y={SlideXmlUtilities.FormatNumber(bounds.Y)} 超出画布顶部 0");
        }

        if (clipToParent && !parentBounds.Contains(bounds))
        {
            warnings.Add($"[Warning] {element.Id}: 元素超出父容器 {parentId}，超出部分将被裁剪");
        }
    }

    private static void DrawElements(DrawingContext context, IReadOnlyList<SlideElement> elements, List<string> warnings)
    {
        foreach (var element in elements)
        {
            DrawElement(context, element, warnings);
        }
    }

    private static void DrawElement(DrawingContext context, SlideElement element, List<string> warnings)
    {
        using var opacity = context.PushOpacity(ClampOpacity(element.Opacity));

        switch (element)
        {
            case SlidePanelElement panel:
                DrawPanel(context, panel, warnings);
                break;
            case SlideRectElement rect:
                DrawRect(context, rect);
                break;
            case SlideTextElement text:
                DrawText(context, text);
                break;
            case SlideImageElement image:
                DrawImage(context, image);
                break;
        }
    }

    private static void DrawPanel(DrawingContext context, SlidePanelElement panel, List<string> warnings)
    {
        var backgroundBrush = CreateBrushFromSlideBrush(panel.BackgroundBrush, panel.LayoutBounds);
        if (backgroundBrush is not null)
        {
            context.FillRectangle(backgroundBrush, panel.LayoutBounds);
        }

        using var clip = context.PushClip(panel.LayoutBounds);
        DrawElements(context, panel.Children, warnings);
    }

    private static void DrawRect(DrawingContext context, SlideRectElement rect)
    {
        var fill = string.IsNullOrWhiteSpace(rect.Fill) ? null : CreateBrush(rect.Fill, Colors.Transparent);
        var pen = string.IsNullOrWhiteSpace(rect.Stroke) || rect.StrokeThickness <= 0
            ? null
            : new Pen(CreateBrush(rect.Stroke, Colors.Transparent), rect.StrokeThickness);

        if (rect.CornerRadius > 0)
        {
            context.DrawRectangle(fill, pen, new RoundedRect(rect.LayoutBounds, rect.CornerRadius));
        }
        else
        {
            context.DrawRectangle(fill, pen, rect.LayoutBounds);
        }
    }

    private static void DrawText(DrawingContext context, SlideTextElement text)
    {
        if (text.TextLayout is null)
        {
            return;
        }

        if (text.Height is double fixedHeight)
        {
            using var clip = context.PushClip(new Rect(text.LayoutBounds.X, text.LayoutBounds.Y, text.LayoutBounds.Width, fixedHeight));
            text.TextLayout.Draw(context, text.LayoutBounds.TopLeft);
        }
        else
        {
            text.TextLayout.Draw(context, text.LayoutBounds.TopLeft);
        }
    }

    private static void DrawImage(DrawingContext context, SlideImageElement image)
    {
        var bounds = image.LayoutBounds;
        if (image.Bitmap is { } bitmap)
        {
            var sourceSize = bitmap.Size;
            var sourceRect = new Rect(0, 0, sourceSize.Width, sourceSize.Height);
            var destinationRect = CalculateImageDestination(bounds, sourceRect, image.Stretch);
            context.DrawImage(bitmap, sourceRect, destinationRect);
            return;
        }

        context.DrawRectangle(new SolidColorBrush(Color.Parse("#FFF8FAFC")), new Pen(new SolidColorBrush(Color.Parse("#FFCBD5E1")), 1), new RoundedRect(bounds, 12));

        var titleLayout = new TextLayout(
            "Image",
            new Typeface(new FontFamily("Microsoft YaHei")),
            22,
            new SolidColorBrush(Color.Parse("#FF64748B")),
            TextAlignment.Center,
            TextWrapping.NoWrap,
            TextTrimming.None,
            null,
            FlowDirection.LeftToRight,
            bounds.Width,
            48,
            28,
            0,
            1);

        var sourceLayout = new TextLayout(
            image.Source,
            new Typeface(new FontFamily("Microsoft YaHei")),
            14,
            new SolidColorBrush(Color.Parse("#FF94A3B8")),
            TextAlignment.Center,
            TextWrapping.Wrap,
            TextTrimming.CharacterEllipsis,
            null,
            FlowDirection.LeftToRight,
            Math.Max(0, bounds.Width - 32),
            Math.Max(0, bounds.Height - 80),
            18,
            0,
            2);

        titleLayout.Draw(context, new Point(bounds.X, bounds.Y + Math.Max(16, bounds.Height * 0.32)));
        sourceLayout.Draw(context, new Point(bounds.X + 16, bounds.Y + Math.Max(48, bounds.Height * 0.32 + 36)));
    }

    private static Rect CalculateImageDestination(Rect destinationBounds, Rect sourceBounds, SlideImageStretch stretch)
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

    private static double ResolveOrigin(double parentOrigin, double parentSize, double elementSize, double? explicitOffset, SlideHorizontalAlignment? alignment)
    {
        if (explicitOffset is double x)
        {
            return parentOrigin + x;
        }

        return alignment switch
        {
            SlideHorizontalAlignment.Center => parentOrigin + Math.Max(0, (parentSize - elementSize) / 2),
            SlideHorizontalAlignment.Right => parentOrigin + Math.Max(0, parentSize - elementSize),
            _ => parentOrigin,
        };
    }

    private static double ResolveOrigin(double parentOrigin, double parentSize, double elementSize, double? explicitOffset, SlideVerticalAlignment? alignment)
    {
        if (explicitOffset is double y)
        {
            return parentOrigin + y;
        }

        return alignment switch
        {
            SlideVerticalAlignment.Center => parentOrigin + Math.Max(0, (parentSize - elementSize) / 2),
            SlideVerticalAlignment.Bottom => parentOrigin + Math.Max(0, parentSize - elementSize),
            _ => parentOrigin,
        };
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

    private static SolidColorBrush CreateBrush(string colorText, Color fallbackColor)
    {
        return new SolidColorBrush(Color.TryParse(colorText, out var color) ? color : fallbackColor);
    }

    private static double ClampOpacity(double opacity)
    {
        if (opacity < 0)
        {
            return 0;
        }

        if (opacity > 1)
        {
            return 1;
        }

        return opacity;
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
        yield return Path.Combine(currentDirectory, source);

        foreach (var extension in new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" })
        {
            yield return Path.Combine(currentDirectory, source + extension);
            yield return Path.Combine(currentDirectory, "Images", source + extension);
            yield return Path.Combine(currentDirectory, "Assets", source + extension);
        }
    }

    private static SlideRenderedMetrics? FindMetrics(SlidePage page, string id)
    {
        foreach (var child in page.Children)
        {
            var metrics = FindMetrics(child, id);
            if (metrics is not null)
            {
                return metrics;
            }
        }

        return null;
    }

    private static SlideRenderedMetrics? FindMetrics(SlideElement element, string id)
    {
        if (string.Equals(element.Id, id, StringComparison.Ordinal))
        {
            return new SlideRenderedMetrics
            {
                ActualWidth = element.ActualWidth,
                ActualHeight = element.ActualHeight,
                ActualLineCount = element is SlideTextElement text ? text.ActualLineCount : null,
            };
        }

        if (element is SlidePanelElement panel)
        {
            foreach (var child in panel.Children)
            {
                var metrics = FindMetrics(child, id);
                if (metrics is not null)
                {
                    return metrics;
                }
            }
        }

        return null;
    }

    private static RenderTargetBitmap RenderErrorPreview(string message)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(CanvasWidth, CanvasHeight));
        using var context = bitmap.CreateDrawingContext();

        context.FillRectangle(new SolidColorBrush(Color.Parse("#FFF8FAFC")), new Rect(0, 0, CanvasWidth, CanvasHeight));
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#FFFCA5A5")), 2), new RoundedRect(new Rect(80, 80, CanvasWidth - 160, CanvasHeight - 160), 20));

        var title = new TextLayout(
            "SlideML Render Error",
            new Typeface(new FontFamily("Microsoft YaHei")),
            32,
            new SolidColorBrush(Color.Parse("#FFB91C1C")),
            TextAlignment.Left,
            TextWrapping.Wrap,
            TextTrimming.None,
            null,
            FlowDirection.LeftToRight,
            CanvasWidth - 240,
            80,
            40,
            0,
            2);

        var details = new TextLayout(
            message,
            new Typeface(new FontFamily("Microsoft YaHei")),
            18,
            new SolidColorBrush(Color.Parse("#FF7F1D1D")),
            TextAlignment.Left,
            TextWrapping.Wrap,
            TextTrimming.CharacterEllipsis,
            null,
            FlowDirection.LeftToRight,
            CanvasWidth - 240,
            CanvasHeight - 260,
            28,
            0,
            10);

        title.Draw(context, new Point(120, 120));
        details.Draw(context, new Point(120, 190));
        return bitmap;
    }
}
