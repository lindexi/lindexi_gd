using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.Segments;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Primitive;
using LightTextEditorPlus.Rendering;
using LightTextEditorPlus.Utils;
using SkiaSharp;

Console.OutputEncoding = Encoding.UTF8;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

const string requestedFamilyName = "微软雅黑";
const float fontSize = 16;
const int bitmapWidth = 1200;
const int bitmapHeight = 400;
string[] glyphSamples =
[
    "a",
    "f",
    "g",
    "A",
    "Ag",
    "中文",
    "测试",
    "Ag中",
    "Agy中",
    "基线",
];
string editorText = "Agf\n中文gA\n基线偏移测试";

using SKTypeface requestedTypeface = SKFontManager.Default.MatchFamily(requestedFamilyName) ?? SKTypeface.Default;
using SKFont probeFont = new(requestedTypeface, fontSize);
using SKPaint probePaint = new()
{
    IsAntialias = true,
    Typeface = requestedTypeface,
    TextSize = fontSize,
    Color = SKColors.Black,
};

Console.WriteLine("=== SkiaSharp 原始字体探针 ===");
Console.WriteLine($"请求字体: {requestedFamilyName}");
Console.WriteLine($"实际字体: {requestedTypeface.FamilyName}");
Console.WriteLine($"字号: {fontSize:0.###}");
Console.WriteLine();

PrintFontMetrics(probeFont);
foreach (string sample in glyphSamples)
{
    PrintTextMeasurement(sample, probeFont, probePaint);
}

string probeImagePath = Path.Combine(AppContext.BaseDirectory, "skia-font-probe.png");
RenderReferenceImage(glyphSamples, probeFont, probePaint, probeImagePath);
Console.WriteLine($"[Artifacts] 原始探针图片: {probeImagePath}");
Console.WriteLine();

Console.WriteLine("=== LightTextEditorPlus Skia 渲染探针 ===");
SkiaTextEditor textEditor = new();
textEditor.TextEditorCore.SetInDebugMode();
textEditor.DisableAutoFlushCaretAndSelectionRender();
textEditor.TextEditorCore.DocumentManager.DocumentWidth = 500;
textEditor.TextEditorCore.DocumentManager.DocumentHeight = 400;
textEditor.TextEditorCore.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");

SkiaTextRunProperty styleRunProperty = SkiaTextRunProperty.FromTextEditor(textEditor.TextEditorCore) with
{
    FontName = new FontName(requestedFamilyName),
    FontSize = fontSize,
    Foreground = new SolidColorSkiaTextBrush(SKColors.Black),
};
textEditor.TextEditorCore.DocumentManager.SetStyleTextRunProperty<SkiaTextRunProperty>(_ => styleRunProperty);
textEditor.AppendText(editorText);
await textEditor.TextEditorCore.WaitLayoutCompletedAsync();
RenderInfoProvider renderInfoProvider = textEditor.TextEditorCore.GetRenderInfo();

PrintDocumentLayout(renderInfoProvider);
PrintParagraphAndLineInfo(renderInfoProvider);
PrintCaretInfos(renderInfoProvider, textEditor, editorText.Length);
PrintSelectionInfo(renderInfoProvider, editorText);
PrintDiagnosisSummary(renderInfoProvider);

string editorImagePath = Path.Combine(AppContext.BaseDirectory, "skia-editor-render.png");
RenderEditorImage(textEditor, renderInfoProvider, editorImagePath, bitmapWidth, bitmapHeight);
Console.WriteLine($"[Artifacts] 编辑器渲染图片: {editorImagePath}");

return;

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

static void PrintDiagnosisSummary(RenderInfoProvider renderInfoProvider)
{
    Console.WriteLine("[DiagnosisSummary]");

    ParagraphRenderInfo firstParagraph = renderInfoProvider.GetParagraphRenderInfoList()[0];
    ParagraphLineRenderInfo firstLine = firstParagraph.GetLineRenderInfoList()[0];
    CharData firstChar = firstLine.Argument.CharList[0];

    TextRect lineBounds = firstLine.ContentBounds;
    TextRect charBounds = firstChar.GetBounds();
    TextRect selectionBounds = renderInfoProvider.GetSelectionBoundsList(new Selection(new CaretOffset(0), new CaretOffset(1)))[0];

    Console.WriteLine($"FirstLine.ContentBounds={FormatTextRect(lineBounds)}");
    Console.WriteLine($"FirstChar.Bounds={FormatTextRect(charBounds)}");
    Console.WriteLine($"Selection[0..1]={FormatTextRect(selectionBounds)}");
    Console.WriteLine($"Delta(CharTop-LineTop)={(charBounds.Y - lineBounds.Y):0.###}");
    Console.WriteLine($"Delta(SelectionTop-LineTop)={(selectionBounds.Y - lineBounds.Y):0.###}");
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

static void PrintDocumentLayout(RenderInfoProvider renderInfoProvider)
{
    DocumentLayoutBounds documentLayoutBounds = renderInfoProvider.GetDocumentLayoutBounds();
    Console.WriteLine("[DocumentLayoutBounds]");
    Console.WriteLine($"Content={FormatTextRect(documentLayoutBounds.DocumentContentBounds)}");
    Console.WriteLine($"Outline={FormatTextRect(documentLayoutBounds.DocumentOutlineBounds)}");
    Console.WriteLine();
}

static void PrintParagraphAndLineInfo(RenderInfoProvider renderInfoProvider)
{
    Console.WriteLine("[Paragraphs]");
    foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
    {
        IParagraphLayoutData paragraphLayoutData = paragraphRenderInfo.ParagraphLayoutData;
        Console.WriteLine($"Paragraph[{paragraphRenderInfo.Index.Index}] Content={FormatTextRect(paragraphLayoutData.TextContentBounds)} Outline={FormatTextRect(paragraphLayoutData.OutlineBounds)}");

        foreach (ParagraphLineRenderInfo lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
        {
            LineDrawingArgument argument = lineRenderInfo.Argument;
            string text = string.Concat(argument.CharList.Select(static c => c.CharObject.ToText()));
            Console.WriteLine($"  Line[{lineRenderInfo.LineIndex}] Text=\"{text}\" Start={FormatTextPoint(argument.StartPoint)} LineContentSize={FormatTextSize(argument.LineContentSize)} LineCharTextSize={FormatTextSize(argument.LineCharTextSize)} Outline={FormatTextRect(lineRenderInfo.OutlineBounds)}");

            for (int i = 0; i < argument.CharList.Count; i++)
            {
                CharData charData = argument.CharList[i];
                TextRect bounds = charData.GetBounds();
                Console.WriteLine($"    Char[{i}] Text=\"{charData.CharObject.ToText()}\" Start={FormatTextPoint(charData.GetStartPoint())} Bounds={FormatTextRect(bounds)} Baseline={charData.Baseline:0.###} Frame={FormatTextSize(charData.Size)} Face={FormatTextSize(charData.FaceSize)} Status={charData.CharDataInfo.Status} GlyphIndex={charData.CharDataInfo.GlyphIndex}");
            }
        }
    }

    Console.WriteLine();
}

static void PrintCaretInfos(RenderInfoProvider renderInfoProvider, SkiaTextEditor textEditor, int textLength)
{
    Console.WriteLine("[CaretRenderInfo]");
    int[] offsets = [0, 1, 2, 3, 4, 5, textLength];
    foreach (int offset in offsets.Distinct())
    {
        if (offset > textLength)
        {
            continue;
        }

        CaretOffset caretOffset = new(offset, isAtLineStart: offset == 0);
        CaretRenderInfo caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(caretOffset, isTestingLineStart: true);
        Console.WriteLine($"Caret Offset={offset} Line={caretRenderInfo.LineIndex} LineCaretOffset={caretRenderInfo.HitLineCaretOffset.Offset} ParagraphOffset={caretRenderInfo.CaretOffset.Offset} IsLineEnd={caretRenderInfo.IsLineEnd} Bounds={FormatTextRect(caretRenderInfo.GetCaretBounds(1))}");
    }

    Console.WriteLine();
}

static void PrintSelectionInfo(RenderInfoProvider renderInfoProvider, string editorText)
{
    Console.WriteLine("[SelectionBounds]");
    Selection[] selections =
    [
        new Selection(new CaretOffset(0), new CaretOffset(Math.Min(2, editorText.Length))),
        new Selection(new CaretOffset(0), new CaretOffset(Math.Min(5, editorText.Length))),
        new Selection(new CaretOffset(1), new CaretOffset(Math.Min(8, editorText.Length))),
        new Selection(new CaretOffset(0), new CaretOffset(editorText.Length)),
    ];

    foreach (Selection selection in selections)
    {
        IReadOnlyList<TextRect> boundsList = renderInfoProvider.GetSelectionBoundsList(selection);
        Console.WriteLine($"Selection {selection.StartOffset.Offset}->{selection.EndOffset.Offset} Count={boundsList.Count}");
        for (int i = 0; i < boundsList.Count; i++)
        {
            Console.WriteLine($"  Rect[{i}]={FormatTextRect(boundsList[i])}");
        }
    }

    Console.WriteLine();
}

static void RenderReferenceImage(IEnumerable<string> samples, SKFont font, SKPaint paint, string imagePath)
{
    const int width = 800;
    const int height = 480;
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

static void RenderEditorImage(SkiaTextEditor textEditor, RenderInfoProvider renderInfoProvider, string imagePath, int width, int height)
{
    using SKBitmap bitmap = new(width, height);
    using SKCanvas canvas = new(bitmap);
    canvas.Clear(SKColors.White);

    ITextEditorContentSkiaRenderer renderer = textEditor.BuildTextEditorSkiaRender(new TextEditorSkiaRenderContext(renderInfoProvider, null));
    renderer.Render(canvas);

    using SKPaint selectionPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1,
        Color = SKColors.Green,
    };

    foreach (TextRect selectionBounds in renderInfoProvider.GetSelectionBoundsList(new Selection(new CaretOffset(0), new CaretOffset(textEditor.TextEditorCore.DocumentManager.CharCount))))
    {
        canvas.DrawRect(selectionBounds.ToSKRect(), selectionPaint);
    }

    using SKImage image = SKImage.FromBitmap(bitmap);
    using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
    using FileStream fileStream = File.Open(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read);
    data.SaveTo(fileStream);
}

static string FormatFloat(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

static string FormatRect(SKRect rect) => $"L={FormatFloat(rect.Left)}, T={FormatFloat(rect.Top)}, R={FormatFloat(rect.Right)}, B={FormatFloat(rect.Bottom)}, W={FormatFloat(rect.Width)}, H={FormatFloat(rect.Height)}";

static string FormatTextPoint(TextPoint point) => $"({point.X:0.###}, {point.Y:0.###})";

static string FormatTextSize(TextSize size) => $"({size.Width:0.###}, {size.Height:0.###})";

static string FormatTextRect(TextRect rect) => $"X={rect.X:0.###}, Y={rect.Y:0.###}, W={rect.Width:0.###}, H={rect.Height:0.###}";