using System;
using System.Collections.Generic;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Rendering;

/// <summary>
/// 水平文本渲染器
/// </summary>
class HorizontalTextRender : TextRenderBase
{
    public override DrawingVisual Render(RenderInfoProvider renderInfoProvider, TextEditor textEditor)
    {
        var drawingVisual = new DrawingVisual();

        var pixelsPerDip = (float) VisualTreeHelper.GetDpi(textEditor).PixelsPerDip;

        using (var drawingContext = drawingVisual.RenderOpen())
        {
            foreach (var paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
            {
                foreach (var lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
                {
                    var argument = lineRenderInfo.Argument;
                    drawingContext.PushTransform(new TranslateTransform(0, argument.StartPoint.Y));

                    try
                    {
                        if (lineRenderInfo.Argument.IsDrawn)
                        {
                            // 如果行已经绘制过，那就尝试复用
                            if (lineRenderInfo.Argument.LineAssociatedRenderData is DrawingGroup cacheLineVisual)
                            {
                                drawingContext.DrawDrawing(cacheLineVisual);
                                continue;
                            }
                        }

                        var lineVisual = DrawLine(argument, pixelsPerDip);
                        lineVisual.Freeze();
                        drawingContext.DrawDrawing(lineVisual);
                        lineRenderInfo.SetDrawnResult(new LineDrawnResult(lineVisual));
                    }
                    finally
                    {
                        drawingContext.Pop();
                    }
                }
            }
        }

        return drawingVisual;
    }

    private IEnumerable<List<CharSpanDrawInfo>> GetCharSpanContinuous(ReadOnlyListSpan<CharData> charList)
    {
        var list = new List<CharSpanDrawInfo>();
        GlyphTypeface? glyphTypeface = null;
        foreach (var charSpanDrawInfo in GetCharSpan(charList))
        {
            if (glyphTypeface is null)
            {
                glyphTypeface = charSpanDrawInfo.GlyphTypeface;
                list.Add(charSpanDrawInfo);
            }
            else if (ReferenceEquals(glyphTypeface, charSpanDrawInfo.GlyphTypeface))
            {
                list.Add(charSpanDrawInfo);
            }
            else
            {
                yield return list;

                glyphTypeface = charSpanDrawInfo.GlyphTypeface;
                list = new List<CharSpanDrawInfo>();
                list.Add(charSpanDrawInfo);
            }
        }

        yield return list;
    }

    private IEnumerable<CharSpanDrawInfo> GetCharSpan(ReadOnlyListSpan<CharData> charList)
    {
        var firstCharData = charList[0];
        var runProperty = firstCharData.RunProperty;
        // 获取到字体信息
        var currentRunProperty = runProperty.AsRunProperty();
        // 获取一个字符，用来进行回滚。即使获取不到，也可以用
        var firstChar = TextContext.DefaultChar;
        var firstText = firstCharData.CharObject.ToText();
        if (firstText.Length > 0)
        {
            firstChar = firstText[0];
        }

        GlyphTypeface glyphTypeface = currentRunProperty.GetGlyphTypeface(firstChar);

        // 可选一段段获取连续的字符
        //charList.GetFirstCharSpanContinuous()
        // 但由于可能存在字体不一致的情况，因此需要逐个字符获取

        foreach (CharData charData in charList)
        {
            var text = charData.CharObject.ToText();
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                // 这里额外处理的情况是，用户设置的字体实际上无法被应用在此字符上。于是就需要执行回滚逻辑
                if (glyphTypeface.CharacterToGlyphMap.TryGetValue(c, out var glyphIndex))
                {
                    var charSpanDrawInfo = new CharSpanDrawInfo(glyphIndex, glyphTypeface, c, charData);
                    yield return charSpanDrawInfo;
                }
                else
                {
                    if (currentRunProperty.TryGetFallbackGlyphTypeface(c, out var fallbackGlyphTypeface, out var fallbackGlyphIndex))
                    {
                        var charSpanDrawInfo = new CharSpanDrawInfo(fallbackGlyphIndex, fallbackGlyphTypeface, c, charData);
                        yield return charSpanDrawInfo;
                    }
                }
            }
        }
    }

    private DrawingGroup DrawLine(in LineDrawingArgument argument, float pixelsPerDip)
    {
        var drawingGroup = new DrawingGroup();
        drawingGroup.GuidelineSet = new GuidelineSet();

        using var drawingContext = drawingGroup.Open();

        // 获取字符属性相同聚合一起的拆分之后的字符
        IEnumerable<ReadOnlyListSpan<CharData>> splitList = argument.CharList.SplitContinuousCharData((last, current) => last.RunProperty.Equals(current.RunProperty));

        foreach (var charList in splitList)
        {
            var firstCharData = charList[0];
            var runProperty = firstCharData.RunProperty;
            // 获取到字体信息
            var currentRunProperty = runProperty.AsRunProperty();
            // 文本字体大小
            var fontSize = runProperty.FontSize;

            // 再拆分为实际渲染可以连续的字符
            foreach (var charSpanDrawInfoList in GetCharSpanContinuous(charList))
            {
                var glyphIndices = new List<ushort>(charSpanDrawInfoList.Count);
                var advanceWidths = new List<double>(charSpanDrawInfoList.Count);
                var characters = new List<char>(charSpanDrawInfoList.Count);

                LightTextEditorPlus.Core.Primitive.Point? startPoint = null;
                // 行渲染高度
                var height = 0d;// argument.Size.Height;
                GlyphTypeface? currentGlyphTypeface = null;

                foreach (var (glyphIndex, glyphTypeface, currentChar, charData) in charSpanDrawInfoList)
                {
                    if (startPoint is null)
                    {
                        startPoint = charData.GetStartPoint();
                        // 为了尽可能的进行复用，尝试减去行的偏移，如此行绘制信息可以重复使用。只需要上层使用重新设置行的偏移量
                        startPoint = startPoint.Value with { Y = startPoint.Value.Y - argument.StartPoint.Y };
                    }

                    currentGlyphTypeface = glyphTypeface;

                    glyphIndices.Add(glyphIndex);

                    var width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                    width = GlyphExtension.RefineValue(width);
                    advanceWidths.Add(width);

                    height = Math.Max(height, glyphTypeface.AdvanceHeights[glyphIndex] * fontSize);

                    characters.Add(currentChar);
                }

                var location = new System.Windows.Point(startPoint!.Value.X, startPoint.Value.Y + height);
                drawingGroup.GuidelineSet.GuidelinesX.Add(location.X);
                drawingGroup.GuidelineSet.GuidelinesY.Add(location.Y);

                var glyphRun = new GlyphRun
                (
                    currentGlyphTypeface,
                    bidiLevel: 0,
                    isSideways: false,
                    renderingEmSize: fontSize,
                    pixelsPerDip: pixelsPerDip,
                    glyphIndices: glyphIndices,
                    baselineOrigin: location, // 设置文本的偏移量
                    advanceWidths: advanceWidths, // 设置每个字符的字宽，也就是字号
                    glyphOffsets: null, // 设置每个字符的偏移量，可以为空
                    characters: characters,
                    deviceFontName: null,
                    clusterMap: null,
                    caretStops: null,
                    language: DefaultXmlLanguage
                );

                Brush brush = currentRunProperty.Foreground.Value;
                drawingContext.DrawGlyphRun(brush, glyphRun);
            }
        }

        return drawingGroup;
    }
}