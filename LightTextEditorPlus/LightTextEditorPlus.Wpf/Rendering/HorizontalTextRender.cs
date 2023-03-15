using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Utils;

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
        GlyphTypeface? glyphTypeface=null;
        foreach (var charSpanDrawInfo in GetCharSpan(charList))
        {
            if (glyphTypeface is null)
            {
                glyphTypeface = charSpanDrawInfo.glyphTypeface;
                list.Add(charSpanDrawInfo);
            }
            else if (ReferenceEquals(glyphTypeface, charSpanDrawInfo.glyphTypeface))
            {
                list.Add(charSpanDrawInfo);
            }
            else
            {
                yield return list;

                glyphTypeface = charSpanDrawInfo.glyphTypeface;
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

        foreach (CharData charData in charList)
        {
            var text = charData.CharObject.ToText();
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (glyphTypeface.CharacterToGlyphMap.TryGetValue(c, out var glyphIndex))
                {
                    var charSpanDrawInfo = new CharSpanDrawInfo(glyphIndex, glyphTypeface, c, charData);
                    yield return charSpanDrawInfo;
                }
                else
                {
                    if (currentRunProperty.TryGetFallbackGlyphTypeface(c,out var fallbackGlyphTypeface,out var fallbackGlyphIndex))
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

        using var drawingContext = drawingGroup.Open();

        IEnumerable<ReadOnlyListSpan<CharData>> splitList = argument.CharList.SplitContinuousCharData((last, current) => last.RunProperty.Equals(current.RunProperty));

        foreach (var charList in splitList)
        {
            var firstCharData = charList[0];
            var runProperty = firstCharData.RunProperty;
            // 获取到字体信息
            var currentRunProperty = runProperty.AsRunProperty();
            // 行渲染高度
            var fontSize = runProperty.FontSize;
          
            foreach (var charSpanDrawInfoList in GetCharSpanContinuous(charList))
            {
                var glyphIndices = new List<ushort>(charSpanDrawInfoList.Count);
                var advanceWidths = new List<double>(charSpanDrawInfoList.Count);
                var characters = new List<char>(charSpanDrawInfoList.Count);

                LightTextEditorPlus.Core.Primitive.Point? startPoint=null;
                // 行渲染高度
                var height = 0d;// argument.Size.Height;
                GlyphTypeface? currentGlyphTypeface=null;

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