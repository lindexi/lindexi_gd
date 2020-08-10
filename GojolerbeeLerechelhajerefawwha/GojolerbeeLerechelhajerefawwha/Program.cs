using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace GojolerbeeLerechelhajerefawwha
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var presentationDocument =
                DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("测试.pptx", false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                var presentation = presentationPart.Presentation;

                // 先获取页面
                var slideIdList = presentation.SlideIdList;

                foreach (var slideId in slideIdList.ChildElements.OfType<SlideId>())
                {
                    // 获取页面内容
                    SlidePart slidePart = (SlidePart) presentationPart.GetPartById(slideId.RelationshipId);

                    var slide = slidePart.Slide;

                    foreach (var shape in
                        slide
                            .Descendants<DocumentFormat.OpenXml.Presentation.Shape>())
                    {
                        var defaultTextStyle = presentation.DefaultTextStyle;
                        var textBody = shape.TextBody;

                        var textBodyListStyle = textBody.ListStyle;

                        Debug.Assert(textBodyListStyle.ChildElements.Count == 0);

                        var paragraph = textBody.Descendants<Paragraph>().First();
                        var level = paragraph.ParagraphProperties?.Level?.Value ?? 1;
                        Debug.Assert(level == 1);
                        Debug.Assert(paragraph.ParagraphProperties == null);

                        var paragraphProperties = defaultTextStyle.Level1ParagraphProperties;

                        foreach (var run in paragraph.Descendants<Run>())
                        {
                            var runProperties = run.RunProperties;
                            var eastAsianFont = runProperties.GetFirstChild<EastAsianFont>();
                            Debug.Assert(eastAsianFont == null);

                            eastAsianFont = paragraphProperties.GetFirstChild<DefaultRunProperties>()
                                .GetFirstChild<EastAsianFont>();

                            var typeface = eastAsianFont.Typeface.Value;

                            Console.WriteLine($"字体是 {typeface}");

                            if (ThemeFontTypePattern.IsMatch(typeface))
                            {
                                // 进入这个分支，字体是 +mn-ea 字体
                                // 这个字体的意思里面 mn 表示 Body 字体
                                // 而 nn 表示 Title 字体，也就是 Major 字体
                                // 后续的 ea 和 lt 等表示采用东亚文字或拉丁文等
                                TextContentType fontType;
                                // mn 的 n 传入字符串是 +mn-ea 也就是第三个字符
                                if (typeface[2] == 'n')
                                {
                                    fontType = TextContentType.Body;
                                }
                                else
                                {
                                    fontType = TextContentType.Title;
                                }

                                FontLang fontLang = FontLang.Unknown;

                                if (typeface.Contains("lt"))
                                {
                                    fontLang = FontLang.LatinFont;
                                }
                                else if (typeface.Contains("cs"))
                                {
                                    fontLang = FontLang.ComplexScriptFont;
                                }
                                else if (typeface.Contains("ea"))
                                {
                                    fontLang = FontLang.EastAsianFont;
                                }

                                // 此时需要获取字体主题
                                var fontScheme = GetFontScheme(slidePart);

                                Debug.Assert(fontScheme != null);

                                FontCollectionType fontCollection = null;
                                if (fontType == TextContentType.Title)
                                {
                                    fontCollection = fontScheme.MajorFont;
                                }
                                else if (fontType == TextContentType.Body)
                                {
                                    fontCollection = fontScheme.MinorFont;
                                }

                                var language = runProperties.Language;

                                Console.WriteLine("字体是" + GetFontFromFontCollection(GetScript(language), fontLang,
                                    fontCollection));
                            }
                            else
                            {
                                // 这就是字体本身了
                                Console.WriteLine("字体是" + typeface);
                            }
                        }
                    }
                }
            }
        }

        private static string GetScript(StringValue language)
        {
            if (language == "zh-CN")
            {
                return "Hans";
            }
            else if (language == "zh-Hant")
            {
                return "Hant";
            }

            // 默认值，当然，在 Office 里面的逻辑是十分多的，需要根据 [国家-地区字符串](https://docs.microsoft.com/zh-CN/cpp/c-runtime-library/country-region-strings?view=vs-2019 ) 文档写代码，只是这部分代码不是本文重点
            return "Hans";
        }

        private static string GetFontFromFontCollection(string scriptTag, FontLang themeTypefaceFontLang,
            FontCollectionType fontCollection)
        {
            if (fontCollection == null)
            {
                return string.Empty;
            }

            // 假定存在对应的语言的字体，那么获取对应的字体
            // 存放方式如
            /*
            <a:majorFont xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
                <a:latin typeface="Calibri Light" panose="020F0302020204030204" />
                <a:ea typeface="" />
                <a:cs typeface="" />
                <a:font script="Jpan" typeface="ＭＳ Ｐゴシック" />
                <a:font script="Hang" typeface="맑은 고딕" />
                <a:font script="Hans" typeface="宋体" />
            </a:majorFont>
            <a:minorFont xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
                <a:latin typeface="Calibri" panose="020F0502020204030204" />
                <a:ea typeface="" />
                <a:cs typeface="" />
                <a:font script="Jpan" typeface="ＭＳ Ｐゴシック" />
                <a:font script="Hang" typeface="맑은 고딕" />
                <a:font script="Hans" typeface="宋体" />
            </a:minorFont>
             */
            // 也就是先尝试获取对应语言的，如果获取不到，就采用语言文化的
            TextFontType textFont = null;
            switch (themeTypefaceFontLang)
            {
                case FontLang.LatinFont:
                    textFont = fontCollection.LatinFont;
                    break;
                case FontLang.EastAsianFont:
                    textFont = fontCollection.EastAsianFont;
                    break;
                case FontLang.ComplexScriptFont:
                    textFont = fontCollection.ComplexScriptFont;
                    break;
                case FontLang.SymbolFont:
                    // 特别不处理，为什么？
                    // 在 fontCollection 是不存在的 SymbolFont 的
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(themeTypefaceFontLang), themeTypefaceFontLang, null);
            }

            var typeface = textFont?.Typeface?.Value;

            if (!string.IsNullOrEmpty(typeface))
            {
                return typeface;
            }

            foreach (var font in fontCollection.Elements<SupplementalFont>())
            {
                // <a:font script="Hang" typeface="맑은 고딕" />
                if (string.Equals(font.Script, scriptTag, StringComparison.OrdinalIgnoreCase))
                {
                    return font.Typeface;
                }
            }

            Debug.Assert(false, "找不到字体");

            return null;
        }

        private static FontScheme GetFontScheme(SlidePart slidePart)
        {
            if (slidePart.ThemeOverridePart?.ThemeOverride?.FontScheme != null)
            {
                return slidePart.ThemeOverridePart.ThemeOverride.FontScheme;
            }

            var slideLayoutPart = slidePart.SlideLayoutPart;

            //从SlideLayout获取theme
            if (slideLayoutPart.ThemeOverridePart?.ThemeOverride?.FontScheme != null)
            {
                return slideLayoutPart.ThemeOverridePart.ThemeOverride.FontScheme;
            }

            var slideMasterPart = slideLayoutPart.SlideMasterPart;

            //从SlideMaster获取theme
            return slideMasterPart?.ThemePart?.Theme?.ThemeElements?.FontScheme;
        }

        private static readonly Regex ThemeFontTypePattern =
            new Regex(@"(^\+(mn|mj)\-(lt|cs|ea)$)", RegexOptions.Compiled);
    }

    /// <summary>
    /// 文字内容的类型
    /// </summary>
    public enum TextContentType
    {
        /// <summary>
        /// 标题
        /// </summary>
        Title,

        /// <summary>
        /// 正文
        /// </summary>
        Body
    }

    public enum FontLang
    {
        /// <summary>
        /// lt
        /// </summary>
        LatinFont,

        /// <summary>
        /// ea
        /// </summary>
        EastAsianFont,

        /// <summary>
        /// cs 
        /// </summary>
        ComplexScriptFont,

        /// <summary>
        /// sym
        /// </summary>
        SymbolFont,

        /// <summary>
        ///  不知道哪个语言
        /// </summary>
        Unknown,
    }
}