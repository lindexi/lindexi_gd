using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace LahallgucheHichawaki;

class TextRenderTestFrameworkElement : FrameworkElement
{
    public TextRenderTestFrameworkElement()
    {
        Width = 200;
        Height = 200;

        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;

        RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
        VisualEdgeMode = EdgeMode.Aliased;
        VisualTextRenderingMode = TextRenderingMode.ClearType;
        VisualTextHintingMode = TextHintingMode.Fixed;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var fontFamily = new FontFamily("微软雅黑");

        Typeface typeface = fontFamily.GetTypefaces().First();
        var maxValue = 0;

        var fontStyle = FontStyles.Normal;
        var fontStretch = FontStretches.Normal;
        var fontWeight = FontWeights.Normal;

        foreach (var temp in fontFamily.GetTypefaces())
        {
            var value = 0;
            if (temp.Style == fontStyle)
            {
                value++;
            }

            if (temp.Stretch == fontStretch)
            {
                value++;
            }

            if (temp.Weight == fontWeight)
            {
                value++;
            }

            if (value > maxValue)
            {
                maxValue = value;
                typeface = temp;
            }
        }

        var success = typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface);
        if (!success)
        {
            Debug.Fail("微软雅黑字体找不到");
        }

        var fontSize = 30;

        var text = "文本测试afgjqiWHXx";
        var glyphIndexList = new List<GlyphInfo>();

        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = (int) text[i]; // 这里的 Code Point 没有处理 Emoji 的高低代理字符
            if (glyphTypeface.CharacterToGlyphMap.TryGetValue(codePoint, out var glyphIndex))
            {
                var width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;
                var height = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;
                glyphIndexList.Add(new GlyphInfo(glyphIndex, width, height));
            }
            else
            {
                // 进入字体回滚
            }
        }

        var pixelsPerDip = (float) VisualTreeHelper.GetDpi(this).PixelsPerDip;

        var baseline = glyphTypeface.Baseline * fontSize;

        var location = new Point(0, baseline);
        drawingContext.PushGuidelineSet(new GuidelineSet([0], [baseline]));

        var defaultXmlLanguage =
            XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);

        var glyphRun = new GlyphRun
        (
            glyphTypeface,
            bidiLevel: 0,
            isSideways: false,
            renderingEmSize: fontSize,
            pixelsPerDip: pixelsPerDip,
            glyphIndices: glyphIndexList.Select(t => t.GlyphIndex).ToList(),
            baselineOrigin: location, // 设置文本的偏移量
            advanceWidths: glyphIndexList.Select(t => t.AdvanceWidth).ToList(), // 设置每个字符的字宽，也就是字号
            glyphOffsets: null, // 设置每个字符的偏移量，可以为空
            characters: text.ToCharArray(),
            deviceFontName: null,
            clusterMap: null,
            caretStops: null,
            language: defaultXmlLanguage
        );

        drawingContext.DrawGlyphRun(Brushes.Black, glyphRun);
    }

    record GlyphInfo(ushort GlyphIndex, double AdvanceWidth, double AdvanceHeight);
}