using System;
using System.Windows.Media;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus;

class CharInfoMeasurer : ICharInfoMeasurer
{
    public CharInfoMeasurer(TextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditor _textEditor;

    public void MeasureAndFillSizeOfRun(in FillSizeOfRunArgument argument)
    {
        CharData currentCharData = argument.CurrentCharData;

        if (currentCharData.Size is not null)
        {
            return;
        }

        var runProperty = currentCharData.RunProperty.AsRunProperty();

        GlyphTypeface glyphTypeface = runProperty.GetGlyphTypeface();
        var fontSize = currentCharData.RunProperty.FontSize;

        // 字外框。文字外框，字外框尺寸
        TextSize textFrameSize;
        // 字面尺寸，字墨尺寸，字墨大小。文字的字身框中，字图实际分布的空间的尺寸
        TextSize textFaceSize;

        if (_textEditor.TextEditorCore.ArrangingType == ArrangingType.Horizontal)
        {
            Utf32CodePoint codePoint = currentCharData.CharObject.CodePoint;
            (textFrameSize, textFaceSize) = MeasureChar(codePoint);

            (TextSize textFrameSize, TextSize faceSize) MeasureChar(Utf32CodePoint c)
            {
                var currentGlyphTypeface = glyphTypeface;
                if (!currentGlyphTypeface.CharacterToGlyphMap.TryGetValue(c.Value, out var glyphIndex))
                {
                    // 居然给定的字体找不到，也就是给定的字符不在当前的字体包含范围里面
                    if (!runProperty.TryGetFallbackGlyphTypeface(c, out currentGlyphTypeface, out glyphIndex))
                    {
                        // 如果连回滚的都没有，那就返回默认方块空格
                        var size = new TextSize(fontSize, fontSize);
                        // 此时只好是字外框和字墨量尺寸相同
                        return (size, size);
                    }
                }

                //var glyphIndex = glyphTypeface.CharacterToGlyphMap[c];
                var width = currentGlyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                width = GlyphExtension.RefineValue(width);
                var height = currentGlyphTypeface.AdvanceHeights[glyphIndex] * fontSize;
                double topSideBearing = currentGlyphTypeface.TopSideBearings[glyphIndex] * fontSize;
                double bottomSideBearing = currentGlyphTypeface.BottomSideBearings[glyphIndex] * fontSize;

                //var pixelsPerDip = (float) VisualTreeHelper.GetDpi(_textEditor).PixelsPerDip;
                //var glyphIndices = new[] { glyphIndex };
                //var advanceWidths = new[] { width };
                //var characters = new[] { c };

                //var location = new System.Windows.Point(0, 0);
                //var glyphRun = new GlyphRun
                //(
                //    glyphTypeface,
                //    bidiLevel: 0,
                //    isSideways: false,
                //    renderingEmSize: fontSize,
                //    pixelsPerDip: pixelsPerDip,
                //    glyphIndices: glyphIndices,
                //    baselineOrigin: location, // 设置文本的偏移量
                //    advanceWidths: advanceWidths, // 设置每个字符的字宽，也就是字号
                //    glyphOffsets: null, // 设置每个字符的偏移量，可以为空
                //    characters: characters,
                //    deviceFontName: null,
                //    clusterMap: null,
                //    caretStops: null,
                //    language: DefaultXmlLanguage
                //);
                //var computeInkBoundingBox = glyphRun.ComputeInkBoundingBox();

                //var matrix = new Matrix();
                //matrix.Translate(location.X, location.Y);
                //computeInkBoundingBox.Transform(matrix);
                ////相对于run.BuildGeometry().Bounds方法，run.ComputeInkBoundingBox()会多出一个厚度为1的框框，所以要减去
                //if (computeInkBoundingBox.Width >= 2 && computeInkBoundingBox.Height >= 2)
                //{
                //    computeInkBoundingBox.Inflate(-1, -1);
                //}

                //var bounds = computeInkBoundingBox;
                // 此方法计算的尺寸远远大于视觉效果

                //// 根据 WPF 行高算法 height = fontSize * fontFamily.LineSpacing
                //// 不等于 glyphTypeface.AdvanceHeights[glyphIndex] * fontSize 的值
                //var fontFamily = new FontFamily("微软雅黑"); // 这里强行使用微软雅黑，只是为了测试
                //height = fontSize * fontFamily.LineSpacing;

                // 根据 PPT 行高算法
                // PPTPixelLineSpacing = (a * PPTFL * OriginLineSpacing + b) * FontSize
                // 其中 PPT 的行距计算的 a 和 b 为一次线性函数的方法，而 PPTFL 是 PPT Font Line Spacing 的意思，在 PPT 所有文字的行距都是这个值
                // 可以将 a 和 PPTFL 合并为 PPTFL 然后使用 a 代替，此时 a 和 b 是常量
                // PPTPixelLineSpacing = (a * OriginLineSpacing + b) * FontSize
                // 常量 a 和 b 的值如下
                // a = 1.2018;
                // b = 0.0034;
                // PPTFontLineSpacing = a;
                //const double pptFontLineSpacing = 1.2018;
                //const double b = 0.0034;
                //const int lineSpacing = 1;

                //height = (pptFontLineSpacing * lineSpacing + b) * height;

                //switch (_textEditor.TextEditorCore.LineSpacingAlgorithm)
                //{
                //    case LineSpacingAlgorithm.WPF:
                //        var fontLineSpacing = runProperty.GetRenderingFontFamily(c).LineSpacing;
                //        height = LineSpacingCalculator.CalculateLineHeightWithWPFLineSpacingAlgorithm(1, height,
                //            fontLineSpacing);
                //        break;
                //    case LineSpacingAlgorithm.PPT:
                //        height = LineSpacingCalculator.CalculateLineHeightWithPPTLineSpacingAlgorithm(1, height);
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException();
                //}

                var fontLineSpacing = runProperty.GetRenderingFontFamily(c).LineSpacing;
                // 使用固定字高，而不是每个字符的字高
                var glyphTypefaceHeight = currentGlyphTypeface.Height * fontSize;
                _ = fontLineSpacing;
                _ = glyphTypefaceHeight;
                _ = height;
                _ = topSideBearing;
                _ = bottomSideBearing;
                //var fakeHeight = height + topSideBearing + bottomSideBearing;
                //_ = fakeHeight;
                // 在以上这些数据上，似乎只有 glyphTypefaceHeight 最正确
                // 但是在 Javanese Text 字体里面，glyphTypefaceHeight=136 显著大于 height=60 导致字符上浮，超过文本框
                //return (bounds.Width, bounds.Height);
                var frameSize = new TextSize(width, glyphTypefaceHeight);
                var faceSize = new TextSize(width, height);
                return (frameSize, faceSize);
            }
        }
        else
        {
            throw new NotImplementedException("还没有实现竖排的文本测量");
        }

        var baseline = glyphTypeface.Baseline * fontSize;
        argument.CharDataLayoutInfoSetter.SetCharDataInfo(currentCharData, textFrameSize, textFaceSize, baseline);
    }
}