using HarfBuzzSharp;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.TextArrayPools;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using LightTextEditorPlus.Core.Primitive.Collections;
using Buffer = HarfBuzzSharp.Buffer;
using Font = HarfBuzzSharp.Font;

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
    //            return new CharInfoMeasureResult(skBounds[0].ToTextRect() with
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

    /// <inheritdoc />
    /// Copy from https://github.com/AvaloniaUI/Avalonia
    /// src\Skia\Avalonia.Skia\TextShaperImpl.cs
    /// src\Skia\Avalonia.Skia\GlyphRunImpl.cs
    public void MeasureAndFillSizeOfRun(in FillSizeOfCharDataListArgument argument)
    {
        UpdateLayoutContext updateLayoutContext = argument.UpdateLayoutContext;
        ICharDataLayoutInfoSetter charDataLayoutInfoSetter = argument.CharDataLayoutInfoSetter;

        CharData currentCharData = argument.CurrentCharData;
        bool isHorizontal = updateLayoutContext.TextEditor.ArrangingType.IsHorizontal;

        if (!currentCharData.IsInvalidCharDataInfo)
        {
            // 已有缓存的尺寸，直接返回即可
            return;
        }

        var charDataList = argument.ToMeasureCharDataList;

        // 获取当前相同的字符属性的段，作为当前一次性测量的段。如此可以提高性能
        charDataList = charDataList.GetFirstCharSpanContinuous();

        var runProperty = currentCharData.RunProperty.AsSkiaRunProperty();

        RenderingRunPropertyInfo renderingRunPropertyInfo = runProperty.GetRenderingRunPropertyInfo(currentCharData.CharObject.CodePoint);

        // 确保设置了字符的尺寸
        // 为什么从 0 开始，而不是 argument.CurrentIndex 开始？原因是在 runList 里面已经使用 Slice 裁剪了
        using CharDataListToCharSpanResult charDataListToCharSpanResult = charDataList.ToRenderCharSpan();

        SKFont skFont = renderingRunPropertyInfo.Font;
        ReadOnlySpan<char> text = charDataListToCharSpanResult.CharSpan;
        List<TextGlyphInfo> glyphInfoList = ShapeByHarfBuzz(text, skFont, updateLayoutContext);

        var charCount = charDataListToCharSpanResult.CharSpan.Length;
        Debug.Assert(charCount == charDataList.Count, "字符数量应该匹配");

        var glyphInfoListCount = glyphInfoList.Count;
        // 为什么是小于等于？因为存在 liga 连写的情况，可能实际的 glyph 数量会小于字符数量
        Debug.Assert(glyphInfoListCount <= charCount);

        using TextPoolArrayContext<ushort> glyphIndexContext = updateLayoutContext.Rent<ushort>(glyphInfoListCount);
        using TextPoolArrayContext<SKRect> glyphBoundsContext = updateLayoutContext.Rent<SKRect>(glyphInfoListCount);

        Span<ushort> glyphIndices = glyphIndexContext.Span;
        Span<SKRect> glyphBounds = glyphBoundsContext.Span;

        using TextPoolArrayContext<SKPoint> renderGlyphPositionsContext = updateLayoutContext.Rent<SKPoint>(glyphInfoListCount);
        var renderGlyphPositions = renderGlyphPositionsContext.Span; // 没有真的用到
        var currentX = 0.0;
        for (int i = 0; i < glyphInfoListCount; i++)
        {
            TextGlyphInfo glyphInfo = glyphInfoList[i];
            var offset = glyphInfo.GlyphOffset;

            // 这里的 GlyphIndex 是 HarfBuzzSharp.Buffer 中的 Codepoint 的值，而不是 GlyphIndex 的值
            glyphIndices[i] = glyphInfo.GlyphIndex;

            renderGlyphPositions[i] = new SKPoint((float) (currentX + offset.OffsetX), (float) offset.OffsetY);

            currentX += glyphInfoList[i].GlyphAdvance;
        }

        // 以下代码仅仅只是为了调试而已，等稳定了就可以删除
        _ = renderGlyphPositions;
        skFont.GetGlyphWidths(glyphIndices, null, glyphBounds);

        // 当前的 run 的边界。这个变量现在没有用到，只有调试用途
        var runBounds = new TextRect();


        var baselineY = -skFont.Metrics.Ascent;

        var baselineOrigin = new SKPoint(0, baselineY);
        currentX = 0.0;

        float charHeight = renderingRunPropertyInfo.GetLayoutCharHeight();

        // 实际使用里面，可以忽略 GetGlyphWidths 的影响，因为实际上没有用到
        using TextPoolArrayContext<CharSizeInfo> charSizeInfoArrayContext = updateLayoutContext.Rent<CharSizeInfo>(glyphInfoListCount);
        Span<CharSizeInfo> charSizeInfoSpan = charSizeInfoArrayContext.Span;

        for (int i = 0; i < glyphInfoListCount; i++)
        {
            var renderBounds = glyphBounds[i];
            TextGlyphInfo glyphInfo = glyphInfoList[i];
            var advance = glyphInfo.GlyphAdvance;

            // 水平布局下，不应该返回字符的渲染高度，而是应该返回字符高度。这样可以保证字符的基线对齐。如 a 和 f 和 g 的高度不相同，则如果将其渲染高度返回，会导致基线不对齐，变成底部对齐
            // 宽度应该是 advance 而不是渲染宽度，渲染宽度太窄

            var width = advance;// renderBounds.Width;
            float height = charHeight;// = renderBounds.Height; //skPaint.TextSize; //(float) skFont.Metrics.Ascent + (float) skFont.Metrics.Descent;

            // 字外框尺寸
            TextSize frameSize = new TextSize(width, height);
            // 字墨尺寸
            TextSize faceSize = new TextSize(renderBounds.Width, renderBounds.Height);
            var nextX = currentX + advance;

            if (!isHorizontal)
            {
                // 计算方法请参阅
                // [WPF 探索 Skia 的竖排文本渲染的字符高度 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/18815810 )
                // 何为 space 属性等，请参阅文档： 《Skia 垂直直排竖排文本字符尺寸间距.enbx》
                float top = renderBounds.Top;
                var space = baselineY + top;
                var renderCharHeight = renderBounds.Height + space;

                frameSize = frameSize with
                {
                    Height = renderCharHeight
                };

                // 对于非横排来说，需要倒换宽度高度，确保竖排也按照横排坐标来计算
                frameSize = frameSize.SwapWidthAndHeight();
                faceSize = faceSize.SwapWidthAndHeight();

                nextX = currentX + faceSize.Width;
            }
            //height = (float) LineSpacingCalculator.CalculateLineHeightWithPPTLineSpacingAlgorithm(1, skPaint.TextSize);
            //var enhance = 0f;
            //// 有些字体的 Top 就是超过格子，不要补偿。如华文仿宋字体
            ////if (baselineY < Math.Abs(skFont.Metrics.Top))
            ////{
            ////    enhance = Math.Abs(skFont.Metrics.Top) - baselineY;
            ////}

            //height = /*skFont.Metrics.Leading + 有些字体的 Leading 是不参与排版的，越过的，属于上加。不能将其加入计算 */ baselineY + skFont.Metrics.Descent + enhance;
            //// 同理 skFont.Metrics.Bottom 也是不应该使用的，可能下加是超过格子的

            var glyphRunBounds = new TextRect((currentX + renderBounds.Left), baselineOrigin.Y + renderBounds.Top, frameSize.Width,
                frameSize.Height);

            Debug.Assert(frameSize == glyphRunBounds.TextSize);

            runBounds = runBounds.Union(glyphRunBounds);

            charSizeInfoSpan[i] = new CharSizeInfo(glyphRunBounds)
            {
                CharDataInfo = new CharDataInfo(frameSize, faceSize, baselineY)
                {
                    GlyphIndex = glyphInfo.GlyphIndex,
                    Status = CharDataInfoStatus.Normal,
                },
                GlyphCluster = glyphInfo.GlyphCluster,
            };

            currentX = nextX;
        }

        if (runBounds.Left < 0)
        {
            runBounds = runBounds.Offset(-runBounds.Left, 0);
        }

        runBounds = runBounds.Offset(baselineOrigin.X, 0);
        _ = runBounds;// 当前 runBounds 只有调试作用

        SetCharDataInfo(charSizeInfoSpan, charDataList, in argument);

        if (currentCharData.IsInvalidCharDataInfo)
        {
            throw new TextEditorInnerException($"测量之后，必然能够获取当前字符的尺寸");
        }
    }

    private static List<TextGlyphInfo> ShapeByHarfBuzz(ReadOnlySpan<char> text, SKFont skFont, UpdateLayoutContext updateLayoutContext)
    {
        SKTypeface skTypeface = skFont.Typeface;
        using var buffer = new Buffer();
        buffer.AddUtf16(text);
        buffer.GuessSegmentProperties();

        buffer.Language = new Language(updateLayoutContext.TextEditor.CurrentCulture);

        using var face = new HarfBuzzSharp.Face(GetTable);

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

        using var font = new HarfBuzzSharp.Font(face);
        font.SetFunctionsOpenType();

        font.Shape(buffer);

        font.GetScale(out var scaleX, out _);

        var fontRenderingEmSize = skFont.Size;
        var textScale = fontRenderingEmSize / (float) scaleX;

        var bufferLength = buffer.Length;

        var glyphInfos = buffer.GetGlyphInfoSpan();

        var glyphPositions = buffer.GetGlyphPositionSpan();

        var glyphInfoList = new List<TextGlyphInfo>(bufferLength);

        for (var i = 0; i < bufferLength; i++)
        {
            var sourceInfo = glyphInfos[i];
            // cluster 字段是一个整数，你可以使用它来帮助识别当字形重排、拆分或组合码点时的情况
            // The cluster field is an integer that you can use to help identify when shaping has reordered, split, or combined code points; we will say more about that in the next chapter.
            uint cluster = sourceInfo.Cluster;

            // ~~这里其实是 Codepoint 的值，而不是 GlyphIndex 的值~~
            // 经过进一步实验测试，发现这里的 Codepoint 就是 GlyphIndex 的值
            // 如 edd88c25d3b92b231df2ed5c7fc0ce9297b98d96 的实验，只要在 HarfBuzzSharp.Font 的 Shape 方法之后，那么获取的 Codepoint 就是 GlyphIndex 的值
            // 如其控制台输出的如下内容：
            // Typeface=Standard Symbols PS GlyphCount=191
            // ContainsGlyph('p')=True 81
            // TryGetGlyph=True 81
            // (int) 'p' == 112
            // Before HarfBuzzSharp.Font.Shape Codepoint=112
            // After HarfBuzzSharp.Font.Shape Codepoint=81
            // 可见在 StandardSymbolsPS.ttf 字体下，在 Shape 之前，获取到的 Codepoint 就是字符的 Codepoint 值，而不是 GlyphIndex 的值。在 Shape 之后，获取到的 Codepoint 就是 GlyphIndex 的值。以下代码是正确的

            var glyphIndex = (ushort) sourceInfo.Codepoint;
            // todo 根据 e5db7d3b8763c1029b67193962b3ac2f73390702 的测试
            // Skia 的字体选取比较残对于 Symbol.ttf 等字体选取将会绘制出方框
            // 解决方法是通过 HarfBuzz 获取 GlyphIndex 带上 SKTextEncoding.GlyphId 进行渲染才能正确

            var position = glyphPositions[i];

            var glyphAdvance = position.XAdvance * textScale;

            var offsetX = position.XOffset * textScale;

            var offsetY = -position.YOffset * textScale;

            glyphInfoList.Add(new TextGlyphInfo(glyphIndex, cluster, glyphAdvance, (offsetX, offsetY)));
        }

        return glyphInfoList;
    }

    private static void SetCharDataInfo
    (
        Span<CharSizeInfo> charSizeInfoSpan,
        TextReadOnlyListSpan<CharData> charDataList,
        in FillSizeOfCharDataListArgument argument
    )
    {
        ICharDataLayoutInfoSetter charDataLayoutInfoSetter = argument.CharDataLayoutInfoSetter;
        Debug.Assert(charSizeInfoSpan.Length>0);
        CharDataInfo ligatureCharDataInfo = charSizeInfoSpan[0].CharDataInfo with
        {
            FrameSize = TextSize.Zero,
            FaceSize = TextSize.Zero,
            GlyphIndex = CharDataInfo.UndefinedGlyphIndex,
            Status = CharDataInfoStatus.LigatureContinue,
        };

        // runListIndex 为什么从 0 开始，而不是 argument.CurrentIndex 开始？原因是在 runList 里面已经使用 Slice 裁剪了
        // 为什么有 i 还不够，还需要 runListIndex 变量？原因是存在 StandardLigatures （liga） 连写的情况，可能实际的 glyph 数量会小于字符数量，这是符合预期的
        int runListIndex = 0;
        for (int i = 0; i < charSizeInfoSpan.Length; i++)
        {
            CharSizeInfo charSizeInfo = charSizeInfoSpan[i];
            CharDataInfo charDataInfo = charSizeInfo.CharDataInfo;

            CharData charData = charDataList[runListIndex];

            // 可能存在 liga 连写的情况
            if (i != charSizeInfoSpan.Length - 1)
            {
                CharSizeInfo nextInfo = charSizeInfoSpan[i + 1];
                if (nextInfo.GlyphCluster > runListIndex + 1)
                {
                    // 证明下一个字形的 cluster 是超过当前字符的下一个字符的
                    // 说明当前字符是 liga 连写的开始
                    for (runListIndex = runListIndex + 1; runListIndex < nextInfo.GlyphCluster; runListIndex++)
                    {
                        SetLigatureContinue(runListIndex);
                    }
                    // 因为循环自带加一的效果，需要冲掉
                    runListIndex--;

                    charDataInfo = charDataInfo with
                    {
                        // 应当设置当前字符为连字开始
                        Status = CharDataInfoStatus.LigatureStart,
                    };
                }
            }
            else
            {
                // 最后一个字符了，如果末尾没有了，则证明最后一个是连字符
                if (runListIndex + 1 < charDataList.Count)
                {
                    for (runListIndex = runListIndex + 1; runListIndex < charDataList.Count; runListIndex++)
                    {
                        SetLigatureContinue(runListIndex);
                    }

                    // 因为循环自带加一的效果，需要冲掉。虽然这是多余的，因为立刻就退出循环了
                    runListIndex--;

                    charDataInfo = charDataInfo with
                    {
                        // 应当设置当前字符为连字开始
                        Status = CharDataInfoStatus.LigatureStart,
                    };
                }
            }

            charDataLayoutInfoSetter.SetCharDataInfo(charData, charDataInfo);
            runListIndex++;
        }

        void SetLigatureContinue(int charIndex)
        {
            CharData ligatureCharData = charDataList[charIndex];
            // 连写的字符，均设置为 LigatureContinue 状态。且无宽度高度值
            charDataLayoutInfoSetter.SetCharDataInfo(ligatureCharData, ligatureCharDataInfo);
        }
    }

    /// <summary>
    /// 文本的字形信息
    /// </summary>
    /// <param name="GlyphIndex"></param>
    /// <param name="GlyphCluster">
    /// cluster 字段是一个整数，你可以使用它来帮助识别当字形重排、拆分或组合码点时的情况
    /// See https://harfbuzz.github.io/shaping-and-shape-plans.html
    /// </param>
    /// <param name="GlyphAdvance"></param>
    /// <param name="GlyphOffset"></param>
    readonly record struct TextGlyphInfo(ushort GlyphIndex, uint GlyphCluster, double GlyphAdvance, (float OffsetX, float OffsetY) GlyphOffset = default);

    /// <summary>
    /// 字符尺寸信息
    /// </summary>
    /// <param name="GlyphRunBounds">字符的外框，字外框</param>
    readonly record struct CharSizeInfo(TextRect GlyphRunBounds)
    {
        /// <summary>
        /// cluster 属性是一个整数，你可以使用它来帮助识别当字形重排、拆分或组合码点时的情况
        /// See https://harfbuzz.github.io/shaping-and-shape-plans.html
        /// </summary>
        public required uint GlyphCluster { get; init; }

        /// <summary>
        /// 字面尺寸，字墨尺寸，字墨大小。文字的字身框中，字图实际分布的空间的尺寸
        /// </summary>
        public TextSize TextFaceSize => CharDataInfo.FaceSize;

        public required CharDataInfo CharDataInfo { get; init; }

        /// <summary>
        /// 文字外框，字外框尺寸
        /// </summary>
        public TextSize TextFrameSize => GlyphRunBounds.TextSize;
    }
}