using System.Globalization;

using SkiaSharp;

const string requestedFamilyName = "微软雅黑";
const float fontSize = 16;

string[] samples =
[
    "a",
    "f",
    "g",
    "A",
    "Ag",
    "中文",
    "测试",
    "Ag中",
];

SKTypeface? typeface = SKFontManager.Default.MatchFamily(requestedFamilyName);
typeface ??= SKTypeface.Default;

using SKFont font = new(typeface, fontSize);
using SKPaint paint = new()
{
    IsAntialias = true,
    Typeface = typeface,
    TextSize = fontSize,
    Color = SKColors.Black,
};

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("=== SkiaSharp 字体探针 ===");
Console.WriteLine($"请求字体: {requestedFamilyName}");
Console.WriteLine($"实际字体: {typeface.FamilyName}");
Console.WriteLine($"字号: {fontSize.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine();

PrintFontMetrics(font);

foreach (string sample in samples)
{
    PrintTextMeasurement(sample, font, paint);
}

string imagePath = Path.Combine(AppContext.BaseDirectory, "skia-font-probe.png");
RenderReferenceImage(samples, font, paint, imagePath);

Console.WriteLine();
Console.WriteLine($"参考图片已输出: {imagePath}");

static void PrintFontMetrics(SKFont font)
{
    SKFontMetrics metrics = font.Metrics;
    float baseline = -metrics.Ascent;
    float lineHeight = metrics.Descent - metrics.Ascent + metrics.Leading;

    Console.WriteLine("[FontMetrics]");
    Console.WriteLine($"Ascent  = {FormatFloat(metrics.Ascent)}");
    Console.WriteLine($"Descent = {FormatFloat(metrics.Descent)}");
    Console.WriteLine($"Top     = {FormatFloat(metrics.Top)}");
    Console.WriteLine($"Bottom  = {FormatFloat(metrics.Bottom)}");
    Console.WriteLine($"Leading = {FormatFloat(metrics.Leading)}");
    Console.WriteLine($"Baseline(=-Ascent) = {FormatFloat(baseline)}");
    Console.WriteLine($"LineHeight         = {FormatFloat(lineHeight)}");
    Console.WriteLine();
}

static void PrintTextMeasurement(string text, SKFont font, SKPaint paint)
{
    SKRect paintBounds = default;
    float measuredWidth = paint.MeasureText(text, ref paintBounds);

    ushort[] glyphs = new ushort[text.Length];
    font.GetGlyphs(text.AsSpan(), glyphs);

    SKRect[] glyphBounds = new SKRect[glyphs.Length];
    float[] glyphWidths = new float[glyphs.Length];
    font.GetGlyphWidths(glyphs, glyphWidths, glyphBounds, paint);
    float baseline = -font.Metrics.Ascent;

    Console.WriteLine($"[Text] \"{text}\"");
    Console.WriteLine($"MeasureText.Width = {FormatFloat(measuredWidth)}");
    Console.WriteLine($"MeasureText.Bounds = {FormatRect(paintBounds)}");
    Console.WriteLine($"Baseline to top    = {FormatFloat(baseline + paintBounds.Top)}");
    Console.WriteLine($"Baseline to bottom = {FormatFloat(baseline + paintBounds.Bottom)}");

    float advanceX = 0;
    for (int i = 0; i < glyphs.Length; i++)
    {
        SKRect bound = glyphBounds[i];
        float width = glyphWidths[i];
        float glyphTopFromFrame = baseline + bound.Top;
        float glyphBottomFromFrame = baseline + bound.Bottom;

        Console.WriteLine(
            $"  Glyph[{i}] Id={glyphs[i]} Advance={FormatFloat(width)} Bounds={FormatRect(bound)} AdvanceX={FormatFloat(advanceX)} TopInFrame={FormatFloat(glyphTopFromFrame)} BottomInFrame={FormatFloat(glyphBottomFromFrame)}");

        advanceX += width;
    }

    Console.WriteLine();
}

static void RenderReferenceImage(IEnumerable<string> samples, SKFont font, SKPaint paint, string imagePath)
{
    const int width = 800;
    const int height = 400;
    const float originX = 40;
    const float startY = 40;
    const float rowHeight = 44;

    using SKBitmap bitmap = new(width, height);
    using SKCanvas canvas = new(bitmap);
    canvas.Clear(SKColors.White);

    using SKPaint linePaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Red,
        StrokeWidth = 1,
        Style = SKPaintStyle.Stroke,
    };

    using SKPaint boundsPaint = new()
    {
        IsAntialias = true,
        Color = SKColors.Blue,
        StrokeWidth = 1,
        Style = SKPaintStyle.Stroke,
    };

    float baselineOffset = -font.Metrics.Ascent;
    int rowIndex = 0;

    foreach (string sample in samples)
    {
        float frameTop = startY + rowIndex * rowHeight;
        float baselineY = frameTop + baselineOffset;

        canvas.DrawLine(0, baselineY, width, baselineY, linePaint);
        canvas.DrawText(sample, originX, baselineY, font, paint);

        SKRect bounds = default;
        paint.MeasureText(sample, ref bounds);
        SKRect absoluteBounds = bounds with
        {
            Left = bounds.Left + originX,
            Right = bounds.Right + originX,
            Top = bounds.Top + baselineY,
            Bottom = bounds.Bottom + baselineY,
        };
        canvas.DrawRect(absoluteBounds, boundsPaint);

        rowIndex++;
    }

    using SKImage image = SKImage.FromBitmap(bitmap);
    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
    using FileStream fileStream = File.Open(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read);
    data.SaveTo(fileStream);
}

static string FormatFloat(float value)
{
    return value.ToString("0.###", CultureInfo.InvariantCulture);
}

static string FormatRect(SKRect rect)
{
    return $"L={FormatFloat(rect.Left)}, T={FormatFloat(rect.Top)}, R={FormatFloat(rect.Right)}, B={FormatFloat(rect.Bottom)}, W={FormatFloat(rect.Width)}, H={FormatFloat(rect.Height)}";
}
