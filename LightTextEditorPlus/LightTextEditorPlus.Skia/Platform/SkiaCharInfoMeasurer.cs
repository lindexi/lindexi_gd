using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;

using HarfBuzzSharp;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;
using SkiaSharp;

using Buffer = HarfBuzzSharp.Buffer;
using Font = HarfBuzzSharp.Font;
using System.Text;
using LightTextEditorPlus.Core.Exceptions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LightTextEditorPlus.Platform;

class SkiaCharInfoMeasurer : ICharInfoMeasurer
{
    public SkiaCharInfoMeasurer()
    {
    }

//    public CharInfoMeasureResult MeasureCharInfo(in CharInfo charInfo)
//    {
//        // 此逻辑只有空行测量才会进入
//        // 以下测量逻辑预计是不正确的，后续也许可以继续看一下具体错在哪

//        SkiaTextRunProperty skiaTextRunProperty = charInfo.RunProperty.AsSkiaRunProperty();

//        RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(charInfo.CharObject.CodePoint);

//        var skTypeface = renderingRunPropertyInfo.Typeface;

//        SKPaint skPaint = renderingRunPropertyInfo.Paint;

//        SKFont skFont = renderingRunPropertyInfo.Font;
//        float baselineY = -skFont.Metrics.Ascent;
//        var textAdvances = skPaint.GetGlyphWidths(charInfo.CharObject.ToText(), out var skBounds);
//        // 使用 GetGlyphWidths 布局也能达到效果，但是其布局效果本身不佳
//        // 暂时没有找到如何对齐基线
//        // 但是在绘制渲染时，自动带上了基线对齐，因此保持 Y 坐标为 0 即可
//        if (skBounds != null && skBounds.Length > 0)
//        {
//            // 为什么实际渲染会感觉超过 11 的值？这是因为 DrawText 的 Point 给的是最下方的坐标，而不是最上方的坐标
//            // 字号是 15 时，测量返回的高度是 11 的值。这是因为这个 11 指的是字符渲染高度
//            //// 这里测量的高度是 11 的值，然而实际渲染是超过 11 的值
//            return new CharInfoMeasureResult(skBounds[0].ToRect() with
//            {
//                X = 0,
//                Y = 0,
//                Width = textAdvances[0],
//                //Height = skBounds[0].Height
//                // 测量的高度是 11 的值，却设置为字体大小 15 的值。刚好渲染 123微软雅黑 时，自动让 123 对齐基线
//                Height = skPaint.TextSize // todo 使用 FontCharHelper 的计算方法
//            }, baselineY);
//        }

//        var asset = skTypeface.OpenStream(out var trueTypeCollectionIndex);
//        var size = asset.Length;
//        var memoryBase = asset.GetMemoryBase();

//        using var blob = new Blob(memoryBase, size, MemoryMode.ReadOnly, () => asset.Dispose());
//        blob.MakeImmutable();

//        using var face = new Face(blob, (uint) trueTypeCollectionIndex);
//        face.UnitsPerEm = skTypeface.UnitsPerEm;

//        var fontSize = charInfo.RunProperty.FontSize;

//        using var font = new Font(face);
//        font.SetFunctionsOpenType();
//        font.GetScale(out var x, out var y);
//        font.SetFunctionsOpenType();

//        float glyphScale = (float) (fontSize / x);

//        using var buffer = new Buffer();
//        buffer.AddUtf32(charInfo.CharObject.ToText());

//        buffer.Direction = Direction.LeftToRight;
//        buffer.Script = Script.Han;
//        buffer.Language = new Language("en");

//        buffer.GuessSegmentProperties();

//        font.Shape(buffer);

//        foreach (GlyphInfo glyphInfo in buffer.GlyphInfos)
//        {
//            uint glyphInfoCodepoint = glyphInfo.Codepoint;
//        }

//        var length = 0f;
//        TextRect bounds = TextRect.Zero;
//        foreach (var glyphPosition in buffer.GlyphPositions)
//        {
//            var left = glyphPosition.XOffset * glyphScale;
//            var top = glyphPosition.YOffset * glyphScale;
//            var width = glyphPosition.XAdvance * glyphScale;
//            var height = glyphPosition.YAdvance * glyphScale;

//            // 预计 height 就是 0 的值
//            // https://github.com/harfbuzz/harfbuzz/discussions/4827
//            if (height == 0)
//            {
//                height = (float) fontSize;
//            }

//            bounds = new TextRect(left, top, width, height);

//            length += glyphPosition.XOffset * glyphScale + glyphPosition.XAdvance * glyphScale;
//        }

//#if DEBUG
//        GC.KeepAlive(length); // 调试代码，仅用于方便在此调试获取其长度/宽度
//#endif

//        return new CharInfoMeasureResult(bounds, baselineY);
//    }

    public void MeasureAndFillSizeOfRun(in FillSizeOfRunArgument argument)
    {
        CharData currentCharData = argument.CurrentCharData;

        if (currentCharData.Size != null)
        {
            // 已有缓存的尺寸，直接返回即可
            return;
        }

        var runList = argument.RunList;

        // 获取当前相同的字符属性的段，作为当前一次性测量的段。如此可以提高性能
        runList = runList.GetFirstCharSpanContinuous();

        UpdateLayoutContext updateLayoutContext = argument.UpdateLayoutContext;
        var runProperty = currentCharData.RunProperty.AsSkiaRunProperty();

        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.GetRenderingRunPropertyInfo(currentCharData.CharObject.CodePoint);

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

                // 确保赋值给每个字符的尺寸
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
                    updateLayoutContext.Logger.LogWarning(Message);
                    // 不能继续循环，否则会出现越界
                    break;
                }
            }
        }

        if (currentCharData.Size == null)
        {
            throw new TextEditorInnerException($"测量之后，必然能够获取当前字符的尺寸");
        }
    }

    private const string Message = "布局过程中发现 CharData 和 Text 数量不匹配，预计是框架内实现的问题";

    readonly record struct TextGlyphInfo(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance, (float OffsetX, float OffsetY) GlyphOffset = default);

}
