using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Rendering;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Document.Decorations;

namespace LightTextEditorPlus.Rendering;

/// <summary>
/// 竖排文本渲染器
/// </summary>
class VerticalTextRenderer : TextRendererBase
{
    public override DrawingVisual Render(RenderInfoProvider renderInfoProvider, TextEditor textEditor)
    {
        var drawingVisual = new DrawingVisual();

        var pixelsPerDip = (float) VisualTreeHelper.GetDpi(textEditor).PixelsPerDip;

        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            foreach (var paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
            {
                foreach (ParagraphLineRenderInfo lineRenderInfo in paragraphRenderInfo.GetLineRenderInfoList())
                {
                    var argument = lineRenderInfo.Argument;
                    drawingContext.PushTransform(new TranslateTransform(argument.StartPoint.X, 0));

                    try
                    {
                        if (lineRenderInfo.Argument.IsDrawn)
                        {
                            if (lineRenderInfo.Argument.LineAssociatedRenderData is DrawingGroup cacheLineVisual)
                            {
                                drawingContext.DrawDrawing(cacheLineVisual);
                                continue;
                            }
                        }

                        var lineVisual = DrawLine(lineRenderInfo, pixelsPerDip, textEditor);
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

    private IEnumerable<CharSpanDrawInfo> GetCharSpan(TextReadOnlyListSpan<CharData> charList)
    {
        var firstCharData = charList[0];
        var runProperty = firstCharData.RunProperty;
        var currentRunProperty = runProperty.AsRunProperty();
        var firstChar = TextContext.DefaultChar;
        var firstText = firstCharData.CharObject.ToText();
        if (firstText.Length > 0)
        {
            firstChar = firstText[0];
        }

        GlyphTypeface glyphTypeface = currentRunProperty.GetGlyphTypeface(firstChar);

        foreach (CharData charData in charList)
        {
            Utf32CodePoint codePoint = charData.CharObject.CodePoint;
            if (glyphTypeface.CharacterToGlyphMap.TryGetValue(codePoint.Value, out var glyphIndex))
            {
                yield return new CharSpanDrawInfo(glyphIndex, glyphTypeface, codePoint, charData);
            }
            else if (currentRunProperty.TryGetFallbackGlyphTypeface(codePoint, out var fallbackGlyphTypeface,
                         out var fallbackGlyphIndex))
            {
                yield return new CharSpanDrawInfo(fallbackGlyphIndex, fallbackGlyphTypeface, codePoint, charData);
            }
        }
    }

    private DrawingGroup DrawLine(ParagraphLineRenderInfo lineRenderInfo, float pixelsPerDip, TextEditor textEditor)
    {
        LineDrawingArgument argument = lineRenderInfo.Argument;

        var drawingGroup = new DrawingGroup();
        drawingGroup.GuidelineSet = new GuidelineSet();

        using var drawingContext = drawingGroup.Open();

        if (lineRenderInfo.IsIncludeMarker)
        {
            TextReadOnlyListSpan<CharData> markerCharDataList = lineRenderInfo.GetMarkerCharDataList();

            if (lineRenderInfo.CurrentParagraph.IsEmptyParagraph)
            {
                drawingContext.PushOpacity(0.3);
            }

            RenderCharList(markerCharDataList);

            if (lineRenderInfo.CurrentParagraph.IsEmptyParagraph)
            {
                drawingContext.Pop();
            }
        }

        IEnumerable<TextReadOnlyListSpan<CharData>> splitList = argument.CharList.SplitContinuousCharData((last, current) => last.RunProperty.Equals(current.RunProperty));

        foreach (var charList in splitList)
        {
            RenderCharList(charList);
        }

        RenderTextDecoration();

        return drawingGroup;

        void RenderCharList(TextReadOnlyListSpan<CharData> charList)
        {
            var firstCharData = charList[0];
            var runProperty = firstCharData.RunProperty;
            var currentRunProperty = runProperty.AsRunProperty();
            var fontSize = runProperty.GetRenderFontSize();
            Brush brush = currentRunProperty.Foreground.Value;

            foreach (var (glyphIndex, glyphTypeface, currentChar, charData) in GetCharSpan(charList))
            {
                TextSize frameSize = charData.Size.SwapWidthAndHeight();
                TextSize faceSize = charData.FaceSize.SwapWidthAndHeight();
                var space = frameSize.Height - faceSize.Height;

                TextPoint startPoint = charData.GetStartPoint();
                double x = startPoint.X;
                double y = startPoint.Y;
                double contentMargin = argument.StartPoint.X - x;

                x -= argument.StartPoint.X;
                if (!textEditor.ArrangingType.IsLeftToRightVertical)
                {
                    x -= argument.LineContentSize.Height - contentMargin;
                }

                var location = new Point(x, y + charData.Baseline - space / 2);
                drawingGroup.GuidelineSet.GuidelinesX.Add(location.X);
                drawingGroup.GuidelineSet.GuidelinesY.Add(location.Y);

                ushort[]? clusterMap = currentChar.CharLength == 2 ? [0, 0] : null;
                double[] advanceWidths = [frameSize.Width];
                ushort[] glyphIndices = [glyphIndex];
                char[] characters = currentChar.ToCharArray();

                var glyphRun = new GlyphRun
                (
                    glyphTypeface,
                    bidiLevel: 0,
                    isSideways: false, // 负责让字形以竖排侧向方式绘制
                    renderingEmSize: fontSize,
                    pixelsPerDip: pixelsPerDip,
                    glyphIndices: glyphIndices,
                    baselineOrigin: location,
                    advanceWidths: advanceWidths,
                    glyphOffsets: null,
                    characters: characters,
                    deviceFontName: null,
                    clusterMap: clusterMap,
                    caretStops: null,
                    language: DefaultXmlLanguage
                );

                drawingContext.DrawGlyphRun(brush, glyphRun);
            }
        }

        void RenderTextDecoration()
        {
            foreach (DecorationSplitResult decorationSplitResult in TextEditorDecorationHelper.SplitContinuousTextDecorationCharData(argument.CharList))
            {
                TextEditorDecoration textEditorDecoration = decorationSplitResult.Decoration;
                RunProperty runProperty = decorationSplitResult.RunProperty;
                TextReadOnlyListSpan<CharData> charDataList = decorationSplitResult.CharList;

                var currentCharDataList = charDataList;
                var currentCharIndexInLine = decorationSplitResult.CurrentCharIndexInLine;
                while (true)
                {
                    TextRect recommendedBounds = TextEditorDecoration
                        .GetDecorationLocationRecommendedBounds(textEditorDecoration.TextDecorationLocation, in currentCharDataList, in lineRenderInfo, textEditor);

                    var decorationArgument = new BuildDecorationArgument()
                    {
                        CharDataList = currentCharDataList,
                        CurrentCharIndexInLine = currentCharIndexInLine,
                        RunProperty = runProperty,
                        LineRenderInfo = lineRenderInfo,
                        TextEditor = textEditor,
                        RecommendedBounds = recommendedBounds
                    };
                    BuildDecorationResult result = textEditorDecoration.BuildDecoration(in decorationArgument);

                    if (result.Drawing is { } drawing)
                    {
                        drawingContext.DrawDrawing(drawing);
                    }

                    if (result.TakeCharCount == currentCharDataList.Count)
                    {
                        break;
                    }
                    else if (result.TakeCharCount < 1)
                    {
                        var message = $"文本装饰渲染时，所需使用的字符数量至少要有一个。装饰器类型： {textEditorDecoration.GetType()}；TakeCharCount={result.TakeCharCount}";
                        if (textEditor.IsInDebugMode)
                        {
                            throw new TextEditorDebugException(message);
                        }
                        else
                        {
                            textEditor.Logger.LogWarning(message);
                        }
                    }
                    else if (result.TakeCharCount > currentCharDataList.Count)
                    {
                        var message = $"文本装饰渲染时，所需使用的字符数量不能超过传入的字符数量。装饰器类型： {textEditorDecoration.GetType()}；TakeCharCount={result.TakeCharCount}；CurrentCharDataListCount={currentCharDataList.Count}";
                        if (textEditor.IsInDebugMode)
                        {
                            throw new TextEditorDebugException(message);
                        }
                        else
                        {
                            textEditor.Logger.LogWarning(message);
                        }
                    }
                    else
                    {
                        currentCharDataList = currentCharDataList.Slice(result.TakeCharCount);
                        currentCharIndexInLine += result.TakeCharCount;
                    }
                }
            }
        }
    }
}
