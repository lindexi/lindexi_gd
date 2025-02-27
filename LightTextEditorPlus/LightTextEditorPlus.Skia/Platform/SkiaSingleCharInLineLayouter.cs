using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using HarfBuzzSharp;

using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils.Maths;
using LightTextEditorPlus.Document;

using SkiaSharp;

using Buffer = HarfBuzzSharp.Buffer;

namespace LightTextEditorPlus.Platform;

internal class SkiaSingleCharInLineLayouter : ISingleCharInLineLayouter
{
    public SkiaSingleCharInLineLayouter(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private SkiaTextEditor TextEditor { get; } // todo 后续考虑弱引用，方便释放
    private TextEditorDebugConfiguration DebugConfiguration => TextEditor.TextEditorCore.DebugConfiguration;
    private ITextLogger Logger => TextEditor.TextEditorCore.Logger;

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        // 获取连续的字符，不连续的字符也不能进入到这里布局。属性不相同的，等待下次进入此方法布局
        TextReadOnlyListSpan<CharData> runList = argument.RunList.Slice(argument.CurrentIndex).GetFirstCharSpanContinuous();
        // 这里的 runList 就是当前准备布局的文本，首个字符就是 CurrentCharData 的值。至少会包含一个字符
        Debug.Assert(runList.Count > 0);
#if DEBUG
        string debugText = runList.ToText();
        _ = debugText;
#endif
        CharData currentCharData = argument.CurrentCharData;
        var runProperty = currentCharData.RunProperty.AsSkiaRunProperty();

        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.GetRenderingRunPropertyInfo(runList[0].CharObject.CodePoint);
        SKTypeface skTypeface = renderingRunPropertyInfo.Typeface;
        SKFont skFont = renderingRunPropertyInfo.Font;
        SKPaint skPaint = renderingRunPropertyInfo.Paint;

        var lineRemainingWidth = (float) argument.LineRemainingWidth;

        // todo 优化以下代码写法
        var stringBuilder = new StringBuilder();
        // 为什么从 0 开始，而不是 argument.CurrentIndex 开始？原因是在 runList 里面已经使用 Slice 裁剪了
        for (var i = 0; i < runList.Count; i++)
        {
            CharData charData = runList[i];
            stringBuilder.Append(charData.CharObject.ToText());
        }

        string text = stringBuilder.ToString();

        // todo 这里需要处理换行规则
        long charCount = skPaint.BreakText(text, lineRemainingWidth, out var measuredWidth);
        var measureCharCount = charCount;
        // 取出的 CharObject 数量
        // 由于 CharObject 和字符不一一对应，可能一个 CharObject 对应多个字符，因此 taskCountOfCharObject 不一定等于 charCount 的值
        int taskCountOfCharObject = 0;
        // 为什么从 0 开始，而不是 argument.CurrentIndex 开始？原因是在 runList 里面已经使用 Slice 裁剪了
        for (var i = 0; i < runList.Count; i++)
        {
            // 这里解决的是可能有一个 CharObject 包含多个 Char 的情况
            CharData charData = runList[i];
            measureCharCount -= charData.CharObject.ToText().Length;
            if (measureCharCount < 0)
            {
                break;
            }
            taskCountOfCharObject++;
        }

        // 是否取了所有的 CharObject 全布局了。当前仅用于调试
        bool takeAllCharObject = taskCountOfCharObject == runList.Count;
        _ = takeAllCharObject;

        // Copy from https://github.com/AvaloniaUI/Avalonia
        // src\Skia\Avalonia.Skia\TextShaperImpl.cs
        // src\Skia\Avalonia.Skia\GlyphRunImpl.cs

        var glyphIndices = new ushort[charCount];
        var glyphBounds = new SKRect[charCount];

        var glyphInfoList = new List<TextGlyphInfo>();
        using (var buffer = new Buffer())
        {
            buffer.AddUtf16(text.AsSpan().Slice(0, (int) charCount));
            buffer.GuessSegmentProperties();

            buffer.Language = new Language(TextEditor.TextEditorCore.CurrentCulture);

            var face = new HarfBuzzSharp.Face(GetTable);

            Blob? GetTable(Face face, Tag tag)
            {
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

                glyphInfoList.Add(new TextGlyphInfo(glyphIndex, glyphCluster, glyphAdvance, (offsetX, offsetY)));
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

        // 以下代码仅仅只是为了调试而已，等稳定了就可以删除
        _ = renderGlyphPositions;

        var runBounds = new SKRect();
        var glyphRunBounds = new SKRect[count];
        skFont.GetGlyphWidths(glyphIndices, null, glyphBounds);

        var baselineY = -skFont.Metrics.Ascent;

        var baselineOrigin = new SKPoint(0, baselineY);
        currentX = 0.0;

        float charHeight = renderingRunPropertyInfo.GetLayoutCharHeight();

        // 实际使用里面，可以忽略 GetGlyphWidths 的影响，因为实际上没有用到
        for (var i = 0; i < count; i++)
        {
            var renderBounds = glyphBounds[i];
            var glyphInfo = glyphInfoList[i];
            var advance = glyphInfo.GlyphAdvance;

            // 水平布局下，不应该返回字符的渲染高度，而是应该返回字符高度。这样可以保证字符的基线对齐。如 a 和 f 和 g 的高度不相同，则如果将其渲染高度返回，会导致基线不对齐，变成底部对齐
            // 宽度应该是 advance 而不是渲染宽度，渲染宽度太窄

            var width = (float) advance;// renderBounds.Width;
            float height = charHeight;// = renderBounds.Height; //skPaint.TextSize; //(float) skFont.Metrics.Ascent + (float) skFont.Metrics.Descent;
            //height = (float) LineSpacingCalculator.CalculateLineHeightWithPPTLineSpacingAlgorithm(1, skPaint.TextSize);
            //var enhance = 0f;
            //// 有些字体的 Top 就是超过格子，不要补偿。如华文仿宋字体
            ////if (baselineY < Math.Abs(skFont.Metrics.Top))
            ////{
            ////    enhance = Math.Abs(skFont.Metrics.Top) - baselineY;
            ////}

            //height = /*skFont.Metrics.Leading + 有些字体的 Leading 是不参与排版的，越过的，属于上加。不能将其加入计算 */ baselineY + skFont.Metrics.Descent + enhance;
            //// 同理 skFont.Metrics.Bottom 也是不应该使用的，可能下加是超过格子的

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
        // 为什么从 0 开始，而不是 argument.CurrentIndex 开始？原因是在 runList 里面已经使用 Slice 裁剪了
        for (var i = 0; i < taskCountOfCharObject; i++)
        {
            CharData charData = runList[i];
            if (charData.Size == null)
            {
                SKRect glyphRunBound = glyphRunBounds[glyphRunBoundsIndex];

                argument.CharDataLayoutInfoSetter.SetCharDataInfo(charData, new TextSize(glyphRunBound.Width, glyphRunBound.Height), baselineY);
            }

            // 解决 CharData 和字符不一一对应的问题，可能一个 CharData 对应多个字符
            glyphRunBoundsIndex += charData.CharObject.ToText().Length;
            // 预期不会出现超出的情况
            if (glyphRunBoundsIndex >= glyphRunBounds.Length)
            {
                if (i == taskCountOfCharObject - 1 && glyphRunBoundsIndex == glyphRunBounds.Length)
                {
                    // 非最后一个。最后一个预期是相等的
                    // 进入这个分支是符合预期的。刚刚好最后一个 CharData 对应的字符刚好是最后一个字符
                    break;
                }

                if (DebugConfiguration.IsInDebugMode)
                {
                    throw new TextEditorInnerDebugException(Message);
                }
                else
                {
                    Logger.LogWarning(Message);
                    // 不能继续循环，否则会出现越界
                    break;
                }
            }
        }

        if (DebugConfiguration.IsInDebugMode && glyphRunBoundsIndex != glyphRunBounds.Length)
        {
            throw new TextEditorInnerDebugException(Message);
        }

        var lineHeight = 0d;
        for (int i = 0; i < taskCountOfCharObject; i++)
        {
            CharData charData = runList[i];
            if (TextEditor.TextEditorCore.IsInDebugMode && charData.Size == null)
            {
                throw new TextEditorDebugException($"经过 Skia 排版之后，字符依然不存在尺寸");
            }

            var currentCharHeight = charData.Size!.Value.Height;
            if (currentCharHeight > lineHeight)
            {
                lineHeight = currentCharHeight;
            }
        }

        if (taskCountOfCharObject > 0)
        {
            Debug.Assert(Math.Abs(charHeight - lineHeight) < 0.1, "正常布局的情况下，行高和字高是相同的。在 Skia 里面所有字的布局字高都是相同的");
        }
        else
        {
            // 没有获取到任何字符的情况下，行高是 0 的值
            Debug.Assert(lineHeight == 0, "没有获取到任何字符的情况下，行高是 0 的值");
        }

        return new SingleCharInLineLayoutResult(taskCountOfCharObject, new TextSize(measuredWidth, lineHeight));
    }

    private const string Message = "布局过程中发现 CharData 和 Text 数量不匹配，预计是框架内实现的问题";

    readonly record struct TextGlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, (float OffsetX, float OffsetY) GlyphOffset = default);
}
