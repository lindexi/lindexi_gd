using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using HarfBuzzSharp;

using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using SkiaSharp;

using Buffer = HarfBuzzSharp.Buffer;

namespace LightTextEditorPlus.Platform;

internal class SkiaSingleCharInLineLayouter : ISingleCharInLineLayouter
{
    public SkiaSingleCharInLineLayouter(SkiaTextEditor textEditor)
    {
        //TextEditor = textEditor;
        DebugConfiguration = textEditor.TextEditorCore.DebugConfiguration;
    }

    //private SkiaTextEditor TextEditor { get; } // todo 后续考虑弱引用，方便释放
    private TextEditorDebugConfiguration DebugConfiguration { get; }

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        CharData currentCharData = argument.CurrentCharData;
        var runProperty = currentCharData.RunProperty.AsSkiaRunProperty();

        // todo 处理连续字符属性的情况
        SKFont skFont = runProperty.GetRenderSKFont();
        // todo 考虑 SKPaint 的复用，不要频繁创建，可以考虑在 SkiaTextEditor 中创建一个 SKPaint 的缓存池
        using SKPaint skPaint = new SKPaint(skFont);
        // skPaint 已经用上 SKFont 的字号属性，不需要再设置 TextSize 属性
        //skPaint.TextSize = runProperty.FontSize;

        var lineRemainingWidth = (float) argument.LineRemainingWidth;

        // todo 优化以下代码写法
        var stringBuilder = new StringBuilder();
        for (var i = argument.CurrentIndex; i < argument.RunList.Count; i++)
        {
            CharData charData = argument.RunList[i];
            stringBuilder.Append(charData.CharObject.ToText());
        }

        string text = stringBuilder.ToString();

        // todo 这里需要处理换行规则
        long charCount = skPaint.BreakText(text, lineRemainingWidth, out var measuredWidth);
        var measureCharCount = charCount;
        int taskCount = 0;
        for (var i = argument.CurrentIndex; i < argument.RunList.Count; i++)
        {
            CharData charData = argument.RunList[i];
            measureCharCount -= charData.CharObject.ToText().Length;
            if (measureCharCount < 0)
            {
                break;
            }
            taskCount++;
        }

        // Copy from https://github.com/AvaloniaUI/Avalonia
        // src\Skia\Avalonia.Skia\TextShaperImpl.cs
        // src\Skia\Avalonia.Skia\GlyphRunImpl.cs

        var glyphIndices = new ushort[charCount];
        var glyphBounds = new SKRect[charCount];

        var glyphInfoList = new List<TestGlyphInfo>();
        using (var buffer = new Buffer())
        {
            buffer.AddUtf16(text.AsSpan().Slice(0, (int) charCount));
            buffer.GuessSegmentProperties();

            // todo 处理语言文化
            buffer.Language = new Language(CultureInfo.CurrentCulture);

            var face = new HarfBuzzSharp.Face(GetTable);

            Blob? GetTable(Face face, Tag tag)
            {
                SKTypeface skTypeface = skFont.Typeface;
                var size = skTypeface.GetTableSize(tag);
                var data = Marshal.AllocCoTaskMem(size);
                if (skTypeface.TryGetTableData(tag, 0, size, data))
                {
                    return new Blob(data, size, MemoryMode.ReadOnly, () => Marshal.FreeCoTaskMem(data));
                }
                else
                {
                    return null;
                }
            }


            var font = new HarfBuzzSharp.Font(face);
            font.SetFunctionsOpenType();

            font.Shape(buffer);

            font.GetScale(out var scaleX, out _);

            var fontRenderingEmSize = skPaint.TextSize;
            var textScale = fontRenderingEmSize / (float) scaleX;

            var bufferLength = buffer.Length;

            var glyphInfos = buffer.GetGlyphInfoSpan();

            var glyphPositions = buffer.GetGlyphPositionSpan();

            for (var i = 0; i < bufferLength; i++)
            {
                var sourceInfo = glyphInfos[i];

                var glyphIndex = (ushort) sourceInfo.Codepoint;

                var glyphCluster = (int) sourceInfo.Cluster;

                var position = glyphPositions[i];

                var glyphAdvance = position.XAdvance * textScale;

                var offsetX = position.XOffset * textScale;

                var offsetY = -position.YOffset * textScale;

                glyphInfoList.Add(new TestGlyphInfo(glyphIndex, glyphCluster, glyphAdvance, (offsetX, offsetY)));
            }
        }

        var count = glyphInfoList.Count;
        var renderGlyphPositions = new SKPoint[count]; // 没有真的用到
        var currentX = 0.0;
        for (int i = 0; i < count; i++)
        {
            var glyphInfo = glyphInfoList[i];
            var offset = glyphInfo.GlyphOffset;

            glyphIndices[i] = glyphInfo.GlyphIndex;

            renderGlyphPositions[i] = new SKPoint((float) (currentX + offset.OffsetX), (float) offset.OffsetY);

            currentX += glyphInfoList[i].GlyphAdvance;
        }

        var runBounds = new SKRect();
        var glyphRunBounds = new SKRect[count];
        skFont.GetGlyphWidths(glyphIndices, null, glyphBounds);

        var baselineY = -skFont.Metrics.Ascent;

        var baselineOrigin = new SKPoint(0, baselineY);
        currentX = 0.0;

        for (var i = 0; i < count; i++)
        {
            var renderBounds = glyphBounds[i];
            var glyphInfo = glyphInfoList[i];
            var advance = glyphInfo.GlyphAdvance;

            // 水平布局下，不应该返回字符的渲染高度，而是应该返回字符高度。这样可以保证字符的基线对齐。如 a 和 f 和 g 的高度不相同，则如果将其渲染高度返回，会导致基线不对齐，变成底部对齐
            // 宽度应该是 advance 而不是渲染宽度，渲染宽度太窄

            var width = (float) advance;// renderBounds.Width;
            var height = renderBounds.Height; //skPaint.TextSize; //(float) skFont.Metrics.Ascent + (float) skFont.Metrics.Descent;
            height = (float) LineSpacingCalculator.CalculateLineHeightWithPPTLineSpacingAlgorithm(1, skPaint.TextSize);

            glyphRunBounds[i] = SKRect.Create((float) (currentX + renderBounds.Left), baselineOrigin.Y + renderBounds.Top, width,
                height);

            runBounds.Union(glyphRunBounds[i]);

            currentX += advance;
        }

        if (runBounds.Left < 0)
        {
            runBounds.Offset(-runBounds.Left, 0);
        }

        runBounds.Offset(baselineOrigin.X, 0);

        // 赋值给每个字符的尺寸
        var glyphRunBoundsIndex = 0;
        for (var i = argument.CurrentIndex; i < argument.CurrentIndex + taskCount; i++)
        {
            CharData charData = argument.RunList[i];
            if (charData.Size == null)
            {
                SKRect glyphRunBound = glyphRunBounds[glyphRunBoundsIndex];

                charData.SetSize(new Size(glyphRunBound.Width, glyphRunBound.Height));
            }

            // 解决 CharData 和字符不一一对应的问题，可能一个 CharData 对应多个字符
            glyphRunBoundsIndex += charData.CharObject.ToText().Length;
            // 预期不会出现超出的情况
            if (glyphRunBoundsIndex >= glyphRunBounds.Length)
            {
                if (DebugConfiguration.IsInDebugMode
                    // 调试下，且非最后一个。最后一个预期是相等的
                    && i != argument.CurrentIndex + taskCount - 1)
                {
                    throw new TextEditorInnerDebugException("布局过程中发现 CharData 和 Text 数量不匹配，预计是框架内实现的问题");
                }
                else
                {
                    break;
                }
            }
        }

        if (DebugConfiguration.IsInDebugMode && glyphRunBoundsIndex != glyphRunBounds.Length)
        {
            throw new TextEditorInnerDebugException("布局过程中发现 CharData 和 Text 数量不匹配，预计是框架内实现的问题");
        }

        return new SingleCharInLineLayoutResult(taskCount, new Size(measuredWidth, 0));
    }

    readonly record struct TestGlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, (float OffsetX, float OffsetY) GlyphOffset = default);
}