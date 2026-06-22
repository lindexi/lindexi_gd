using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator;

/// <summary>
/// 渲染引擎 WPF 实现，包含全部 UI 框架依赖。
/// 内部维护 FormattedText 和 Bitmap 缓存，避免污染数据模型。
/// </summary>
internal sealed class WpfSlideMlRenderEngine : ISlideMlRenderEngine
{
    private readonly Dictionary<string, FormattedText> _formattedTextCache = new();
    private readonly Dictionary<string, List<FormattedText>> _spanFormattedTextCache = new();
    private readonly Dictionary<string, BitmapSource?> _bitmapCache = new();

    /// Helper: convert SlideMlRect to WPF Rect
    private static Rect ToRect(SlideMlRect r) => new(r.X, r.Y, r.Width, r.Height);
    private static Point ToPoint(SlideMlRect r, double offsetX = 0, double offsetY = 0) => new(r.X + offsetX, r.Y + offsetY);

    /// <inheritdoc />
    public SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        _formattedTextCache.Clear();
        _spanFormattedTextCache.Clear();
        _bitmapCache.Clear();

        var results = new Dictionary<string, SlideMlMeasureResult>();
        PreMeasureElements(page.Children, results, context);
        return new SlideMlElementMeasurements(results);
    }

    /// <inheritdoc />
    public IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        var bitmap = new RenderTargetBitmap(context.CanvasWidth, context.CanvasHeight, 96.0, 96.0, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(CreateBrush(page.Background, Colors.White), null,
                new Rect(0, 0, context.CanvasWidth, context.CanvasHeight));
            DrawElements(dc, page.Children, context);
        }
        bitmap.Render(visual);
        return new WpfPreviewImage(bitmap);
    }

    /// <inheritdoc />
    public IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context)
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
        return new WpfPreviewImage(bitmap);
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
        var fontWeight = MapIsBold(text.IsBold);
        var foreground = CreateBrush(text.Foreground, Colors.Black);

        var maxWidth = text.Width ?? 10000;
        var maxHeight = text.Height ?? 10000;
        var lineHeight = text.FontSize * text.LineHeight;

        if (text.Spans is { Count: > 0 })
        {
            // 富文本 Span：逐段测量并拼接
            PreMeasureSpans(text, results, maxWidth, maxHeight, lineHeight);
            return;
        }

        var fontStyle = MapIsItalic(text.IsItalic);

        var typeface = new Typeface(new FontFamily(text.FontName), fontStyle, fontWeight, FontStretches.Normal);

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

        results[text.Id] = new SlideMlMeasureResult
        {
            MeasuredWidth = measuredWidth,
            MeasuredHeight = measuredHeight,
            ActualLineCount = actualLineCount,
        };
    }

    /// <summary>
    /// 预测量富文本 Span，逐段创建 FormattedText 并拼接。
    /// </summary>
    private void PreMeasureSpans(SlideMlTextElement text, Dictionary<string, SlideMlMeasureResult> results,
        double maxWidth, double maxHeight, double lineHeight)
    {
        var spans = text.Spans!;
        var formattedTexts = new List<FormattedText>(spans.Count);

        double totalWidth = 0;
        double totalHeight = 0;

        foreach (var span in spans)
        {
            var spanFontWeight = MapIsBold(span.IsBold ?? text.IsBold);
            var spanFontName = span.FontName ?? text.FontName;
            var spanFontSize = span.FontSize ?? text.FontSize;
            var spanForeground = CreateBrush(span.Foreground ?? text.Foreground, Colors.Black);
            var spanFontStyle = MapIsItalic(span.IsItalic ?? text.IsItalic);

            var spanTypeface = new Typeface(new FontFamily(spanFontName), spanFontStyle, spanFontWeight, FontStretches.Normal);

            var ft = new FormattedText(
                span.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                spanTypeface,
                spanFontSize,
                spanForeground,
                GetDpi())
            {
                TextAlignment = MapTextAlignment(text.TextAlignment),
                MaxTextWidth = text.Width is null ? 0 : Math.Max(0, maxWidth - totalWidth),
                MaxTextHeight = maxHeight,
                LineHeight = lineHeight,
            };

            formattedTexts.Add(ft);
            totalWidth += ft.WidthIncludingTrailingWhitespace;
            totalHeight = Math.Max(totalHeight, ft.Height);
        }

        // 合并 FormattedText 到缓存，用于后续 DrawText
        _formattedTextCache[text.Id] = CombineFormattedTexts(formattedTexts);

        var measuredWidth = text.Width ?? totalWidth;
        var measuredHeight = text.Height ?? totalHeight;
        var actualLineCount = totalHeight > 0 ? (int)Math.Ceiling(totalHeight / lineHeight) : 0;
        if (actualLineCount == 0 && spans.Count > 0)
        {
            actualLineCount = 1;
        }

        // 保存各个 Span 的 FormattedText 以便绘制
        _spanFormattedTextCache[text.Id] = formattedTexts;

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

        var measuredWidth = image.Width ?? (bitmap?.PixelWidth ?? 240);
        var measuredHeight = image.Height ?? (bitmap?.PixelHeight ?? 180);

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
            var radius = rect.CornerRadius;
            DrawShadow(dc, rect.Shadow, ToRect(rect.LayoutBounds), radius);
        }

        var opacity = Math.Clamp(element.Opacity, 0, 1);
        dc.PushOpacity(opacity);

        switch (element)
        {
            case SlideMlPanelElement panel:
                DrawPanel(dc, panel, context);
                break;
            case SlideMlRectElement rect2:
                DrawRect(dc, rect2);
                break;
            case SlideMlTextElement text:
                DrawText(dc, text);
                break;
            case SlideMlImageElement image:
                DrawImage(dc, image);
                break;
        }

        dc.Pop();
    }

    private void DrawPanel(DrawingContext dc, SlideMlPanelElement panel, SlideMlPipelineContext context)
    {
        var backgroundBrush = CreateWpfBrush(panel.Background, ToRect(panel.LayoutBounds), Colors.Transparent);

        if (backgroundBrush is not null)
        {
            dc.DrawRectangle(backgroundBrush, null, ToRect(panel.LayoutBounds));
        }

        dc.PushClip(new RectangleGeometry(ToRect(panel.LayoutBounds)));
        DrawElements(dc, panel.Children, context);
        dc.Pop();
    }

    private static void DrawRect(DrawingContext dc, SlideMlRectElement rect)
    {
        var bounds = ToRect(rect.LayoutBounds);

        var fillBrush = CreateWpfBrush(rect.Fill, bounds, Colors.Transparent);

        Pen? pen = null;
        if (rect.StrokeThickness > 0)
        {
            var strokeBrush = CreateWpfBrush(rect.Stroke, bounds, Colors.Transparent);
            if (strokeBrush is not null)
            {
                pen = new Pen(strokeBrush, rect.StrokeThickness);

                if (rect.StrokeDashArray is { Count: > 0 })
                {
                    pen.DashStyle = new DashStyle(rect.StrokeDashArray, 0);
                }
            }
        }

        DrawRoundedRect(dc, fillBrush, pen, bounds, rect.CornerRadius);
    }

    /// <summary>
    /// 绘制支持四角独立圆角的矩形。单值圆角走快速路径。
    /// </summary>
    private static void DrawRoundedRect(DrawingContext dc, Brush? fill, Pen? pen, Rect bounds, SlideMlCornerRadius? radius)
    {
        if (radius is null)
        {
            dc.DrawRectangle(fill, pen, bounds);
            return;
        }

        var r = radius.Value;
        var hasDifferentRadii = Math.Abs(r.TopLeft - r.TopRight) > 0.01
            || Math.Abs(r.TopLeft - r.BottomRight) > 0.01
            || Math.Abs(r.TopLeft - r.BottomLeft) > 0.01;

        if (!hasDifferentRadii)
        {
            var uniformRadius = Math.Max(0, r.TopLeft);
            if (uniformRadius > 0)
            {
                dc.DrawRoundedRectangle(fill, pen, bounds, uniformRadius, uniformRadius);
            }
            else
            {
                dc.DrawRectangle(fill, pen, bounds);
            }

            return;
        }

        // 四角独立圆角 — 使用 StreamGeometry 构建路径
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var x = bounds.X;
            var y = bounds.Y;
            var w = bounds.Width;
            var h = bounds.Height;
            var maxRadius = Math.Min(w / 2, h / 2);

            var tl = Math.Clamp(r.TopLeft, 0, maxRadius);
            var tr = Math.Clamp(r.TopRight, 0, maxRadius);
            var br = Math.Clamp(r.BottomRight, 0, maxRadius);
            var bl = Math.Clamp(r.BottomLeft, 0, maxRadius);

            ctx.BeginFigure(new Point(x + tl, y), fill is not null, true);
            // 上边 → 右上角
            ctx.LineTo(new Point(x + w - tr, y), true, true);
            if (tr > 0)
            {
                ctx.ArcTo(new Point(x + w, y + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise, true, true);
            }

            // 右边 → 右下角
            ctx.LineTo(new Point(x + w, y + h - br), true, true);
            if (br > 0)
            {
                ctx.ArcTo(new Point(x + w - br, y + h), new Size(br, br), 0, false, SweepDirection.Clockwise, true, true);
            }

            // 下边 → 左下角
            ctx.LineTo(new Point(x + bl, y + h), true, true);
            if (bl > 0)
            {
                ctx.ArcTo(new Point(x, y + h - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise, true, true);
            }

            // 左边 → 左上角
            ctx.LineTo(new Point(x, y + tl), true, true);
            if (tl > 0)
            {
                ctx.ArcTo(new Point(x + tl, y), new Size(tl, tl), 0, false, SweepDirection.Clockwise, true, true);
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(fill, pen, geometry);
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
            bounds.X + shadow.OffsetX - shadow.Blur,
            bounds.Y + shadow.OffsetY - shadow.Blur,
            bounds.Width + shadow.Blur * 2,
            bounds.Height + shadow.Blur * 2);

        if (shadowBounds.Width <= 0 || shadowBounds.Height <= 0)
        {
            return;
        }

        var shadowColor = CreateColor(shadow.Color, Colors.Black);
        shadowColor.A = (byte)(shadowColor.A * Math.Clamp(shadow.Opacity, 0, 1));
        var shadowBrush = new SolidColorBrush(shadowColor);

        // 使用 BlurEffect 方式：先渲染到 DrawingVisual，再应用模糊
        var visual = new DrawingVisual();
        using (var shadowDc = visual.RenderOpen())
        {
            var uniformRadius = radius?.TopLeft ?? 0;
            if (uniformRadius > 0)
            {
                shadowDc.DrawRoundedRectangle(shadowBrush, null, shadowBounds, uniformRadius, uniformRadius);
            }
            else
            {
                shadowDc.DrawRectangle(shadowBrush, null, shadowBounds);
            }
        }

        if (shadow.Blur > 0)
        {
            visual.Effect = new System.Windows.Media.Effects.BlurEffect
            {
                Radius = shadow.Blur,
                KernelType = System.Windows.Media.Effects.KernelType.Gaussian,
            };
        }

        // 将 visual 渲染到目标 DrawingContext
        var renderTarget = new RenderTargetBitmap(
            (int)Math.Ceiling(shadowBounds.Right + shadow.Blur),
            (int)Math.Ceiling(shadowBounds.Bottom + shadow.Blur),
            96.0, 96.0, PixelFormats.Pbgra32);
        renderTarget.Render(visual);

        dc.DrawImage(renderTarget, new Rect(shadowBounds.X - shadow.Blur, shadowBounds.Y - shadow.Blur,
            shadowBounds.Width + shadow.Blur * 2, shadowBounds.Height + shadow.Blur * 2));
    }

    private void DrawText(DrawingContext dc, SlideMlTextElement text)
    {
        // 富文本 Span 渲染
        if (_spanFormattedTextCache.TryGetValue(text.Id, out var spanTexts) && spanTexts is { Count: > 0 })
        {
            DrawSpans(dc, text, spanTexts);
            return;
        }

        if (!_formattedTextCache.TryGetValue(text.Id, out var formattedText) || formattedText is null)
        {
            return;
        }

        if (text.Height is double fixedHeight)
        {
            dc.PushClip(new RectangleGeometry(new Rect(text.LayoutBounds.X, text.LayoutBounds.Y, text.LayoutBounds.Width, fixedHeight)));
            dc.DrawText(formattedText, new Point(text.LayoutBounds.X, text.LayoutBounds.Y));
            dc.Pop();
        }
        else
        {
            dc.DrawText(formattedText, new Point(text.LayoutBounds.X, text.LayoutBounds.Y));
        }
    }

    /// <summary>
    /// 逐段绘制富文本 Span，每段独立样式。
    /// </summary>
    private void DrawSpans(DrawingContext dc, SlideMlTextElement text, List<FormattedText> spanTexts)
    {
        var x = text.LayoutBounds.X;
        var y = text.LayoutBounds.Y;

        if (text.Height is double fixedHeight)
        {
            dc.PushClip(new RectangleGeometry(new Rect(x, y, text.LayoutBounds.Width, fixedHeight)));
        }

        foreach (var ft in spanTexts)
        {
            dc.DrawText(ft, new Point(x, y));
            x += ft.WidthIncludingTrailingWhitespace;
        }

        if (text.Height is not null)
        {
            dc.Pop();
        }
    }

    private void DrawImage(DrawingContext dc, SlideMlImageElement image)
    {
        var bounds = ToRect(image.LayoutBounds);
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

    internal static Rect CalculateImageDestination(Rect destinationBounds, Rect sourceBounds, SlideMlImageStretch stretch)
    {
        if (stretch == SlideMlImageStretch.Fill || sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
        {
            return destinationBounds;
        }

        if (stretch == SlideMlImageStretch.None)
        {
            return new Rect(destinationBounds.X, destinationBounds.Y, Math.Min(sourceBounds.Width, destinationBounds.Width), Math.Min(sourceBounds.Height, destinationBounds.Height));
        }

        var scaleX = destinationBounds.Width / sourceBounds.Width;
        var scaleY = destinationBounds.Height / sourceBounds.Height;
        var scale = stretch == SlideMlImageStretch.Uniform ? Math.Min(scaleX, scaleY) : Math.Max(scaleX, scaleY);
        var width = sourceBounds.Width * scale;
        var height = sourceBounds.Height * scale;
        var x = destinationBounds.X + (destinationBounds.Width - width) / 2;
        var y = destinationBounds.Y + (destinationBounds.Height - height) / 2;
        return new Rect(x, y, width, height);
    }

    internal static TextAlignment MapTextAlignment(SlideMlTextAlignment textAlignment)
    {
        return textAlignment switch
        {
            SlideMlTextAlignment.Center => TextAlignment.Center,
            SlideMlTextAlignment.Right => TextAlignment.Right,
            SlideMlTextAlignment.Justify => TextAlignment.Justify,
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

    /// <summary>
        /// 将多个 FormattedText 拼接为一个（用于简单文本回退时）。
        /// </summary>
        private static FormattedText CombineFormattedTexts(List<FormattedText> texts)
        {
            if (texts.Count == 1)
            {
                return texts[0];
            }

            // 使用第一个段落作为模板，合并文本
            var combined = texts[0];
            // 对于 span 回退，使用第一个元素作为占位
            return combined;
        }

        /// <summary>
        /// 将 <see cref="ISlideMlBrush"/> 转换为 WPF <see cref="Brush"/>。
        /// 纯色画刷创建 <see cref="SolidColorBrush"/>，渐变画刷创建 <see cref="LinearGradientBrush"/>。
        /// </summary>
        internal static Brush? CreateWpfBrush(ISlideMlBrush? brush, Rect bounds, Color fallbackColor)
        {
            return brush switch
            {
                SlideMlSolidColorBrush solid => CreateBrush(solid.Color, fallbackColor),
                SlideMlLinearGradientBrush gradient => CreateGradientBrush(gradient, bounds),
                _ => null,
            };
        }

        /// <summary>
        /// 将 <see cref="SlideMlLinearGradientBrush"/> 转换为 WPF <see cref="LinearGradientBrush"/>。
        /// </summary>
        internal static LinearGradientBrush? CreateGradientBrush(SlideMlLinearGradientBrush? gradient, Rect bounds)
        {
            if (gradient is null || gradient.Stops.Count == 0)
            {
                return null;
            }

            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(gradient.X1, gradient.Y1),
                EndPoint = new Point(gradient.X2, gradient.Y2),
                MappingMode = BrushMappingMode.RelativeToBoundingBox,
            };

            foreach (var stop in gradient.Stops)
            {
                brush.GradientStops.Add(new GradientStop(
                    CreateColor(stop.Color, Colors.Transparent),
                    stop.Offset));
            }

            return brush;
        }

        /// <summary>
        /// 将 IsBold 转换为 WPF <see cref="FontWeight"/>。
        /// </summary>
        internal static FontWeight MapIsBold(bool? isBold)
        {
            return isBold == true ? FontWeights.Bold : FontWeights.Normal;
        }

        /// <summary>
        /// 将 IsItalic 转换为 WPF <see cref="FontStyle"/>。
        /// </summary>
        internal static FontStyle MapIsItalic(bool? isItalic)
        {
            return isItalic == true ? FontStyles.Italic : FontStyles.Normal;
        }

        /// <summary>
        /// 从颜色字符串创建 <see cref="Color"/>，失败返回后备值。
        /// </summary>
        internal static Color CreateColor(string colorText, Color fallbackColor)
        {
            if (!string.IsNullOrWhiteSpace(colorText))
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(colorText);
                }
                catch (FormatException)
                {
                }
            }

            return fallbackColor;
        }

        private static double GetDpi()
    {
        return VisualTreeHelper.GetDpi(Application.Current.MainWindow ?? Application.Current.Windows.OfType<Window>().FirstOrDefault()).PixelsPerDip;
    }
}
