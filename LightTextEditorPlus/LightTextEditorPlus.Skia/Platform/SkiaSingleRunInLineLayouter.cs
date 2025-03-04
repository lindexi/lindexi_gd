using System;
using System.Collections.Generic;
using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive.Collections;

using SkiaSharp;

using HarfBuzzSharp;
using Buffer = HarfBuzzSharp.Buffer;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Platform;

// 其实这里是不需要的，只要有字符布局即可
public class SkiaWholeLineCharsLayouter : IWholeLineCharsLayouter
{
    public WholeLineCharsLayoutResult UpdateWholeLineCharsLayout(in WholeLineLayoutArgument argument)
    {
        ParagraphProperty paragraphProperty = argument.ParagraphProperty;
        TextReadOnlyListSpan<CharData> charDataList = argument.CharDataList;
        double lineMaxWidth = argument.LineMaxWidth;
        UpdateLayoutContext context = argument.UpdateLayoutContext;

        IWordDivider wordDivider = context.PlatformProvider.GetWordDivider();

        // 行还剩余的空闲宽度
        double lineRemainingWidth = lineMaxWidth;

        // 当前相对于 charDataList 的当前序号
        int currentIndex = 0;
        // 当前的字符布局尺寸
        var currentSize = TextSize.Zero;

        while (currentIndex < charDataList.Count)
        {
            TextReadOnlyListSpan<CharData> runList = charDataList.Slice(currentIndex).GetFirstCharSpanContinuous();
            var arguments = new SingleCharInLineLayoutArgument(charDataList, currentIndex, lineRemainingWidth,
                argument.Paragraph, context);

            FillSizeOfCharData(runList, in arguments);

            // 使用断行算法计算是否需要断行
            var currentRunList = charDataList.Slice(currentIndex);

            // 从当前的字符开始，尝试获取一个单词
            DivideWordResult divideWordResult = wordDivider.DivideWord(new DivideWordArgument(currentRunList, context));
            int takeCount = divideWordResult.TakeCount;

            // 计算当前的总宽度
            var takeSize = TextSize.Zero;
            for (int i = 0; i < takeCount; i++)
            {
                CharData charData = currentRunList[i];
                TextSize size = charData.Size ?? throw new InvalidOperationException("CharData 的 Size 不能为空");
                takeSize = takeSize.HorizontalUnion(size);
            }

            if (lineRemainingWidth > takeSize.Width)
            {
                // 这一行还有空间，可以继续放入单词
                lineRemainingWidth -= takeSize.Width;

                currentIndex += takeCount;

                currentSize = currentSize.HorizontalUnion(takeSize);
            }
            else
            {
                // 这一行没有空间了，需要断行
                if (currentIndex == 0)
                {
                    // 这一行一个单词都放不下，那就强行放入一个个字符
                    for (; currentIndex < currentRunList.Count; currentIndex++)
                    {
                        var charData = currentRunList[currentIndex];
                        TextSize textSize = charData.Size ?? throw new InvalidOperationException("CharData 的 Size 不能为空");

                        if (lineRemainingWidth > textSize.Width)
                        {
                            lineRemainingWidth -= textSize.Width;
                            currentSize = currentSize.HorizontalUnion(textSize);
                            // currentIndex++
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                break;
            }
        }

        int totalTakeCount = currentIndex;

        return new WholeLineCharsLayoutResult(currentSize, totalTakeCount);
    }

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        return MeasureSingleRunLayout(in argument);
    }

    private SingleCharInLineLayoutResult MeasureSingleRunLayout(in SingleCharInLineLayoutArgument argument)
    {
        // 获取连续的字符，不连续的字符也不能进入到这里布局。属性不相同的，等待下次进入此方法布局
        TextReadOnlyListSpan<CharData> runList = argument.RunList.Slice(argument.CurrentIndex).GetFirstCharSpanContinuous();

        FillSizeOfCharData(runList, in argument);

        throw new NotImplementedException();
    }

    private static void FillSizeOfCharData(TextReadOnlyListSpan<CharData> runList, in SingleCharInLineLayoutArgument argument)
    {
        UpdateLayoutContext updateLayoutContext = argument.UpdateLayoutContext;

        CharData currentCharData = argument.CurrentCharData;
        var runProperty = currentCharData.RunProperty.AsSkiaRunProperty();

        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.GetRenderingRunPropertyInfo(runList[0].CharObject.CodePoint);
        SKTypeface skTypeface = renderingRunPropertyInfo.Typeface;
        SKFont skFont = renderingRunPropertyInfo.Font;
        SKPaint skPaint = renderingRunPropertyInfo.Paint;

        // 确保设置了字符的尺寸
        var charCount = 0;
        // 为什么从 0 开始，而不是 argument.CurrentIndex 开始？原因是在 runList 里面已经使用 Slice 裁剪了
        StringBuilder stringBuilder = new StringBuilder(runList.Count);
        for (var i = 0; i < runList.Count; i++)
        {
            // 这里解决的是可能有一个 CharObject 包含多个 Char 的情况
            CharData charData = runList[i];
            string text = charData.CharObject.ToText();
            charCount += text.Length;
            stringBuilder.Append(text);
        }

        // Copy from https://github.com/AvaloniaUI/Avalonia
        // src\Skia\Avalonia.Skia\TextShaperImpl.cs
        // src\Skia\Avalonia.Skia\GlyphRunImpl.cs

        var glyphIndices = new ushort[charCount];
        var glyphBounds = new SKRect[charCount];

        var glyphInfoList = new List<TextGlyphInfo>();
        using (var buffer = new Buffer())
        {
            var text = stringBuilder.ToString();
            buffer.AddUtf16(text);
            buffer.GuessSegmentProperties();

            buffer.Language = new Language(updateLayoutContext.TextEditor.CurrentCulture);

            var face = new HarfBuzzSharp.Face(GetTable);

            Blob? GetTable(Face f, Tag tag)
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
        skFont.GetGlyphWidths(glyphIndices.AsSpan(0, charCount), null, glyphBounds.AsSpan(0, charCount));

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
        for (var i = 0; i < runList.Count; i++)
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
                if (i == runList.Count - 1 && glyphRunBoundsIndex == glyphRunBounds.Length)
                {
                    // 非最后一个。最后一个预期是相等的
                    // 进入这个分支是符合预期的。刚刚好最后一个 CharData 对应的字符刚好是最后一个字符
                    break;
                }

                if (updateLayoutContext.IsInDebugMode)
                {
                    throw new TextEditorInnerDebugException(Message);
                }
                else
                {
                    updateLayoutContext. Logger.LogWarning(Message);
                    // 不能继续循环，否则会出现越界
                    break;
                }
            }
        }
    }

    private const string Message = "布局过程中发现 CharData 和 Text 数量不匹配，预计是框架内实现的问题";
    readonly record struct TextGlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, (float OffsetX, float OffsetY) GlyphOffset = default);

   
}