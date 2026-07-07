using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace CoursewarePptxGeneratorWpfDemo.Rendering;

/// <summary>
/// 课件 WPF 渲染引擎实现，负责将 SlideML 页面渲染为预览图。
/// </summary>
internal sealed class CoursewareWpfSlideMlRenderEngine : ISlideMlRenderEngine
{
    private readonly Dictionary<string, FormattedText> _formattedTextCache = new();
    private readonly Dictionary<string, List<FormattedText>> _spanFormattedTextCache = new();
    private readonly Dictionary<string, BitmapSource?> _bitmapCache = new();
    private readonly bool _enableClip = false;

    /// <inheritdoc />
    public SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        _formattedTextCache.Clear();
        _spanFormattedTextCache.Clear();
        _bitmapCache.Clear();

        var results = new Dictionary<string, SlideMlElementMeasureResult>();
        PreMeasureElements(page.Children, results, context);
        return new SlideMlElementMeasurements(results);
    }

    /// <inheritdoc />
    public IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        var bitmap = new RenderTargetBitmap(
            context.SlideDocumentContext.CanvasWidth,
            context.SlideDocumentContext.CanvasHeight,
            96.0,
            96.0,
            PixelFormats.Pbgra32);
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(
                CreateBrush(page.Background, Colors.White),
                null,
                new Rect(0, 0, context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight));
            DrawElements(dc, page.Children, context);
        }

        bitmap.Render(visual);
        return new CoursewareWpfPreviewImage(bitmap);
    }

    /// <inheritdoc />
    public IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);

        var bitmap = new RenderTargetBitmap(
            context.SlideDocumentContext.CanvasWidth,
            context.SlideDocumentContext.CanvasHeight,
            96.0,
            96.0,
            PixelFormats.Pbgra32);
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(CreateBrush("#FFF8FAFC", Colors.White), null, new Rect(0, 0, context.SlideDocumentContext.CanvasWidth, context.SlideDocumentContext.CanvasHeight));
            dc.DrawRectangle(null, new Pen(CreateBrush("#FFFCA5A5", Colors.Red), 2), new Rect(80, 80, context.SlideDocumentContext.CanvasWidth - 160, context.SlideDocumentContext.CanvasHeight - 160));

            var titleText = CreateFormattedText(
                "SlideML Render Error",
                "Microsoft YaHei",
                FontStyles.Normal,
                FontWeights.Normal,
                32,
                CreateBrush("#FFB91C1C", Colors.Red));
            titleText.TextAlignment = TextAlignment.Left;
            titleText.MaxTextWidth = context.SlideDocumentContext.CanvasWidth - 240;
            titleText.MaxTextHeight = 80;
            titleText.LineHeight = 40;
            titleText.Trimming = TextTrimming.None;

            var detailsText = CreateFormattedText(
                message,
                "Microsoft YaHei",
                FontStyles.Normal,
                FontWeights.Normal,
                18,
                CreateBrush("#FF7F1D1D", Colors.DarkRed));
            detailsText.TextAlignment = TextAlignment.Left;
            detailsText.MaxTextWidth = context.SlideDocumentContext.CanvasWidth - 240;
            detailsText.MaxTextHeight = context.SlideDocumentContext.CanvasHeight - 260;
            detailsText.LineHeight = 28;
            detailsText.Trimming = TextTrimming.CharacterEllipsis;

            dc.DrawText(titleText, new Point(120, 120));
            dc.DrawText(detailsText, new Point(120, 190));
        }

        bitmap.Render(visual);
        return new CoursewareWpfPreviewImage(bitmap);
    }

    private static Rect ToRect(SlideMlRect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    private void PreMeasureElements(IReadOnlyList<SlideMlElement> elements, Dictionary<string, SlideMlElementMeasureResult> results, SlideMlPipelineContext context)
    {
        foreach (var element in elements)
        {
            PreMeasureElement(element, results, context);
        }
    }

    private void PreMeasureElement(SlideMlElement element, Dictionary<string, SlideMlElementMeasureResult> results, SlideMlPipelineContext context)
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

    private void PreMeasureText(SlideMlTextElement text, Dictionary<string, SlideMlElementMeasureResult> results)
    {
        var maxWidth = text.Width ?? 10000;
        var maxHeight = text.Height ?? 10000;
        var lineHeight = text.FontSize;

        if (text.Spans is { Count: > 0 })
        {
            PreMeasureSpans(text, results, maxWidth, maxHeight, lineHeight);
            return;
        }

        var formattedText = CreateFormattedText(
            text.Text,
            text.FontName,
            MapIsItalic(text.IsItalic),
            MapIsBold(text.IsBold),
            text.FontSize,
            CreateBrush(text.Foreground, Colors.Black));

        formattedText.TextAlignment = MapTextAlignment(text.TextAlignment);
        formattedText.MaxTextWidth = text.Width is null ? 0 : maxWidth;
        formattedText.MaxTextHeight = maxHeight;
        formattedText.LineHeight = lineHeight;

        _formattedTextCache[text.Id] = formattedText;
        results[text.Id] = new SlideMlElementMeasureResult
        {
            MeasuredWidth = text.Width ?? formattedText.WidthIncludingTrailingWhitespace,
            MeasuredHeight = text.Height ?? formattedText.Height,
            ActualLineCount = CalculateLineCount(formattedText.Height, lineHeight, text.Text.Length > 0),
        };
    }

    private void PreMeasureSpans(SlideMlTextElement text, Dictionary<string, SlideMlElementMeasureResult> results, double maxWidth, double maxHeight, double lineHeight)
    {
        var spans = text.Spans!;
        var formattedTexts = new List<FormattedText>(spans.Count);
        double totalWidth = 0;
        double totalHeight = 0;

        foreach (var span in spans)
        {
            var formattedText = CreateFormattedText(
                span.Text,
                span.FontName ?? text.FontName,
                MapIsItalic(span.IsItalic ?? text.IsItalic),
                MapIsBold(span.IsBold ?? text.IsBold),
                span.FontSize ?? text.FontSize,
                CreateBrush(span.Foreground ?? text.Foreground, Colors.Black));

            formattedText.TextAlignment = MapTextAlignment(text.TextAlignment);
            formattedText.MaxTextWidth = text.Width is null ? 0 : Math.Max(0, maxWidth - totalWidth);
            formattedText.MaxTextHeight = maxHeight;
            formattedText.LineHeight = lineHeight;

            formattedTexts.Add(formattedText);
            totalWidth += formattedText.WidthIncludingTrailingWhitespace;
            totalHeight = Math.Max(totalHeight, formattedText.Height);
        }

        _formattedTextCache[text.Id] = CombineFormattedTexts(formattedTexts);
        _spanFormattedTextCache[text.Id] = formattedTexts;
        results[text.Id] = new SlideMlElementMeasureResult
        {
            MeasuredWidth = text.Width ?? totalWidth,
            MeasuredHeight = text.Height ?? totalHeight,
            ActualLineCount = CalculateLineCount(totalHeight, lineHeight, spans.Count > 0),
        };
    }

    private void PreMeasureImage(SlideMlImageElement image, Dictionary<string, SlideMlElementMeasureResult> results, SlideMlPipelineContext context)
    {
        var bitmap = TryLoadBitmap(image.Source);
        _bitmapCache[image.Id] = bitmap;

        if (bitmap is null)
        {
            context.AddWarning($"[Warning] {image.Id}: 图片资源 {image.Source} 未找到，已使用占位图");
        }

        results[image.Id] = new SlideMlElementMeasureResult
        {
            MeasuredWidth = image.Width ?? (bitmap?.PixelWidth ?? 240),
            MeasuredHeight = image.Height ?? (bitmap?.PixelHeight ?? 180),
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
        if (element is SlideMlRectElement { Shadow: not null } shadowRect)
        {
            DrawShadow(dc, shadowRect.Shadow, ToRect(shadowRect.LayoutBounds), shadowRect.CornerRadius);
        }

        dc.PushOpacity(Math.Clamp(element.Opacity, 0, 1));
        try
        {
            switch (element)
            {
                case SlideMlPanelElement panel:
                    DrawPanel(dc, panel, context);
                    break;
                case SlideMlRectElement rectElement:
                    DrawRect(dc, rectElement);
                    break;
                case SlideMlTextElement text:
                    DrawText(dc, text);
                    break;
                case SlideMlImageElement image:
                    DrawImage(dc, image);
                    break;
            }
        }
        finally
        {
            dc.Pop();
        }
    }

    private void DrawPanel(DrawingContext dc, SlideMlPanelElement panel, SlideMlPipelineContext context)
    {
        var backgroundBrush = CoursewareWpfSlideMlBrushConverter.CreateWpfBrush(panel.Background);
        if (backgroundBrush is not null)
        {
            dc.DrawRectangle(backgroundBrush, null, ToRect(panel.LayoutBounds));
        }

        using (PushClip(dc, panel.LayoutBounds.X, panel.LayoutBounds.Y, panel.LayoutBounds.Width, panel.LayoutBounds.Height))
        {
            DrawElements(dc, panel.Children, context);
        }
    }

    private static void DrawRect(DrawingContext dc, SlideMlRectElement rect)
    {
        var fillBrush = CoursewareWpfSlideMlBrushConverter.CreateWpfBrush(rect.Fill);
        Pen? pen = null;
        if (rect.StrokeThickness > 0)
        {
            var strokeBrush = CoursewareWpfSlideMlBrushConverter.CreateWpfBrush(rect.Stroke);
            if (strokeBrush is not null)
            {
                pen = new Pen(strokeBrush, rect.StrokeThickness);
                if (rect.StrokeDashArray is { Count: > 0 })
                {
                    pen.DashStyle = new DashStyle(rect.StrokeDashArray, 0);
                }
            }
        }

        DrawRoundedRect(dc, fillBrush, pen, ToRect(rect.LayoutBounds), rect.CornerRadius);
    }

    private static void DrawRoundedRect(DrawingContext dc, Brush? fill, Pen? pen, Rect bounds, SlideMlCornerRadius? radius)
    {
        if (radius is null)
        {
            dc.DrawRectangle(fill, pen, bounds);
            return;
        }

        var r = radius.Value;
        if (Math.Abs(r.TopLeft - r.TopRight) <= 0.01 && Math.Abs(r.TopLeft - r.BottomRight) <= 0.01 && Math.Abs(r.TopLeft - r.BottomLeft) <= 0.01)
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
            ctx.LineTo(new Point(x + w - tr, y), true, true);
            if (tr > 0) ctx.ArcTo(new Point(x + w, y + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise, true, true);
            ctx.LineTo(new Point(x + w, y + h - br), true, true);
            if (br > 0) ctx.ArcTo(new Point(x + w - br, y + h), new Size(br, br), 0, false, SweepDirection.Clockwise, true, true);
            ctx.LineTo(new Point(x + bl, y + h), true, true);
            if (bl > 0) ctx.ArcTo(new Point(x, y + h - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise, true, true);
            ctx.LineTo(new Point(x, y + tl), true, true);
            if (tl > 0) ctx.ArcTo(new Point(x + tl, y), new Size(tl, tl), 0, false, SweepDirection.Clockwise, true, true);
        }

        geometry.Freeze();
        dc.DrawGeometry(fill, pen, geometry);
    }

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

        var renderTarget = new RenderTargetBitmap(
            (int)Math.Ceiling(shadowBounds.Right + shadow.Blur),
            (int)Math.Ceiling(shadowBounds.Bottom + shadow.Blur),
            96.0,
            96.0,
            PixelFormats.Pbgra32);
        renderTarget.Render(visual);
        dc.DrawImage(renderTarget, new Rect(shadowBounds.X - shadow.Blur, shadowBounds.Y - shadow.Blur, shadowBounds.Width + shadow.Blur * 2, shadowBounds.Height + shadow.Blur * 2));
    }

    private void DrawText(DrawingContext dc, SlideMlTextElement text)
    {
        if (_spanFormattedTextCache.TryGetValue(text.Id, out var spanTexts) && spanTexts.Count > 0)
        {
            DrawSpans(dc, text, spanTexts);
            return;
        }

        if (!_formattedTextCache.TryGetValue(text.Id, out var formattedText))
        {
            return;
        }

        if (text.Height is double fixedHeight)
        {
            using (PushClip(dc, text.LayoutBounds.X, text.LayoutBounds.Y, text.LayoutBounds.Width, fixedHeight))
            {
                dc.DrawText(formattedText, new Point(text.LayoutBounds.X, text.LayoutBounds.Y));
            }
        }
        else
        {
            dc.DrawText(formattedText, new Point(text.LayoutBounds.X, text.LayoutBounds.Y));
        }
    }

    private void DrawSpans(DrawingContext dc, SlideMlTextElement text, IReadOnlyList<FormattedText> spanTexts)
    {
        var x = text.LayoutBounds.X;
        var y = text.LayoutBounds.Y;
        ClipScope? clipScope = text.Height is double fixedHeight ? PushClip(dc, x, y, text.LayoutBounds.Width, fixedHeight) : null;

        try
        {
            foreach (var formattedText in spanTexts)
            {
                dc.DrawText(formattedText, new Point(x, y));
                x += formattedText.WidthIncludingTrailingWhitespace;
            }
        }
        finally
        {
            clipScope?.Dispose();
        }
    }

    private void DrawImage(DrawingContext dc, SlideMlImageElement image)
    {
        var bounds = ToRect(image.LayoutBounds);
        if (_bitmapCache.TryGetValue(image.Id, out var bitmap) && bitmap is not null)
        {
            var sourceRect = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
            dc.DrawImage(bitmap, CalculateImageDestination(bounds, sourceRect, image.Stretch));
            return;
        }

        dc.DrawRectangle(CreateBrush("#FFF8FAFC", Colors.White), new Pen(CreateBrush("#FFCBD5E1", Colors.Gray), 1), bounds);
        var titleText = CreateFormattedText("Image", "Microsoft YaHei", FontStyles.Normal, FontWeights.Normal, 22, CreateBrush("#FF64748B", Colors.Gray));
        titleText.TextAlignment = TextAlignment.Center;
        titleText.MaxTextWidth = bounds.Width;
        titleText.MaxTextHeight = 48;
        titleText.LineHeight = 28;

        var sourceText = CreateFormattedText(image.Source, "Microsoft YaHei", FontStyles.Normal, FontWeights.Normal, 14, CreateBrush("#FF94A3B8", Colors.Gray));
        sourceText.TextAlignment = TextAlignment.Center;
        sourceText.MaxTextWidth = Math.Max(0, bounds.Width - 32);
        sourceText.MaxTextHeight = Math.Max(0, bounds.Height - 80);
        sourceText.LineHeight = 18;
        sourceText.Trimming = TextTrimming.CharacterEllipsis;

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
        return new Rect(destinationBounds.X + (destinationBounds.Width - width) / 2, destinationBounds.Y + (destinationBounds.Height - height) / 2, width, height);
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
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorText));
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

    internal static FontWeight MapIsBold(bool? isBold) => isBold == true ? FontWeights.Bold : FontWeights.Normal;

    internal static FontStyle MapIsItalic(bool? isItalic) => isItalic == true ? FontStyles.Italic : FontStyles.Normal;

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

    private static FormattedText CreateFormattedText(string text, string fontName, FontStyle fontStyle, FontWeight fontWeight, double fontSize, Brush foreground)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(fontName), fontStyle, fontWeight, FontStretches.Normal),
            fontSize,
            foreground,
            GetDpi());
    }

    private static FormattedText CombineFormattedTexts(IReadOnlyList<FormattedText> texts)
    {
        if (texts.Count == 0)
        {
            return CreateFormattedText(string.Empty, "Microsoft YaHei", FontStyles.Normal, FontWeights.Normal, 1, Brushes.Transparent);
        }

        return texts[0];
    }

    private static int CalculateLineCount(double height, double lineHeight, bool hasContent)
    {
        var actualLineCount = lineHeight > 0 ? (int)Math.Ceiling(height / lineHeight) : 0;
        return actualLineCount == 0 && hasContent ? 1 : actualLineCount;
    }

    private static double GetDpi()
    {
        var visual = Application.Current?.MainWindow ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault();
        return visual is null ? 1.0 : VisualTreeHelper.GetDpi(visual).PixelsPerDip;
    }

    private ClipScope PushClip(DrawingContext dc, double x, double y, double width, double height)
    {
        if (!_enableClip)
        {
            return new ClipScope(dc, pushed: false);
        }

        dc.PushClip(new RectangleGeometry(new Rect(x, y, width, height)));
        return new ClipScope(dc, pushed: true);
    }

    private struct ClipScope : IDisposable
    {
        private readonly DrawingContext _dc;
        private readonly bool _pushed;
        private bool _disposed;

        internal ClipScope(DrawingContext dc, bool pushed)
        {
            _dc = dc;
            _pushed = pushed;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed || !_pushed)
            {
                return;
            }

            _dc.Pop();
            _disposed = true;
        }
    }
}
