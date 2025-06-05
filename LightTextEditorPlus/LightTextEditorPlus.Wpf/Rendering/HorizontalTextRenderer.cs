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
/// 水平文本渲染器
/// </summary>
class HorizontalTextRenderer : TextRendererBase
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

    private IEnumerable<List<CharSpanDrawInfo>> GetCharSpanContinuous(TextReadOnlyListSpan<CharData> charList)
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

    private IEnumerable<CharSpanDrawInfo> GetCharSpan(TextReadOnlyListSpan<CharData> charList)
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
            Utf32CodePoint codePoint = charData.CharObject.CodePoint;
            // 这里额外处理的情况是，用户设置的字体实际上无法被应用在此字符上。于是就需要执行回滚逻辑
            if (glyphTypeface.CharacterToGlyphMap.TryGetValue(codePoint.Value, out var glyphIndex))
            {
                var charSpanDrawInfo = new CharSpanDrawInfo(glyphIndex, glyphTypeface, codePoint, charData);
                yield return charSpanDrawInfo;
            }
            else
            {
                if (currentRunProperty.TryGetFallbackGlyphTypeface(codePoint, out var fallbackGlyphTypeface, out var fallbackGlyphIndex))
                {
                    var charSpanDrawInfo = new CharSpanDrawInfo(fallbackGlyphIndex, fallbackGlyphTypeface, codePoint, charData);
                    yield return charSpanDrawInfo;
                }
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
            // 如果包含了项目符号，那么就需要先绘制项目符号
            TextReadOnlyListSpan<CharData> markerCharDataList = lineRenderInfo.GetMarkerCharDataList();

            if (lineRenderInfo.CurrentParagraph.IsEmptyParagraph)
            {
                // 空段落的情况，修改一下透明度
                drawingContext.PushOpacity(0.3);
            }

            RenderCharList(markerCharDataList);

            if (lineRenderInfo.CurrentParagraph.IsEmptyParagraph)
            {
                // 空段落的情况，修改一下透明度
                drawingContext.Pop();
            }
        }

        // 获取字符属性相同聚合一起的拆分之后的字符
        IEnumerable<TextReadOnlyListSpan<CharData>> splitList = argument.CharList.SplitContinuousCharData((last, current) => last.RunProperty.Equals(current.RunProperty));

        var renderList = splitList;

        foreach (var charList in renderList)
        {
            RenderCharList(charList);
        }

        // 渲染文本装饰
        RenderTextDecoration();

        return drawingGroup;

        void RenderCharList(TextReadOnlyListSpan<CharData> charList)
        {
            var firstCharData = charList[0];
            var runProperty = firstCharData.RunProperty;
            // 获取到字体信息
            var currentRunProperty = runProperty.AsRunProperty();
            // 文本字体大小
            var fontSize = runProperty.GetRenderFontSize();

            // 再拆分为实际渲染可以连续的字符
            foreach (var charSpanDrawInfoList in GetCharSpanContinuous(charList))
            {
                var glyphIndices = new List<ushort>(charSpanDrawInfoList.Count);
                var advanceWidths = new List<double>(charSpanDrawInfoList.Count);
                var characters = new List<char>(charSpanDrawInfoList.Count);

                LightTextEditorPlus.Core.Primitive.TextPoint? startPoint = null;
                // 行渲染高度
                var height = 0d;// argument.Size.Height;
                var baseline = 0d;
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

                    baseline = Math.Max(baseline, glyphTypeface.Baseline * fontSize);

                    currentChar.AppendToCharList(characters);
                }

                var location = new System.Windows.Point(startPoint!.Value.X, startPoint.Value.Y + baseline);
                drawingGroup.GuidelineSet.GuidelinesX.Add(location.X);
                drawingGroup.GuidelineSet.GuidelinesY.Add(location.Y);

                ushort[]? clusterMap = null;
                if (glyphIndices.Count != characters.Count)
                {
                    clusterMap = new ushort[characters.Count];

                    var clusterMapIndex = 0;
                    for (ushort i = 0; i < charSpanDrawInfoList.Count; i++)
                    {
                        Utf32CodePoint utf32CodePoint = charSpanDrawInfoList[i].CurrentChar;
                        int charLength = utf32CodePoint.CharLength;

                        clusterMap[clusterMapIndex] = i;
                        clusterMapIndex++;

                        if (charLength == 2)
                        {
                            clusterMap[clusterMapIndex] = i;
                            clusterMapIndex++;
                        }
                    }
                }

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
                    clusterMap: clusterMap,
                    caretStops: null,
                    language: DefaultXmlLanguage
                );

                Brush brush = currentRunProperty.Foreground.Value;
                drawingContext.DrawGlyphRun(brush, glyphRun);
            }
        }

        void RenderTextDecoration()
        {
            // todo 后续考虑文本装饰在另一个层绘制，规避 EdgeMode.Aliased 导致的着重号圆点有棱角的问题
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
                    else if(result.TakeCharCount < 1)
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
