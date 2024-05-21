using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P15 = DocumentFormat.OpenXml.Office2013.PowerPoint;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonPresentationPartGenerator
    {
        public static PresentationPart GeneratePresentationPart(this PresentationDocument document)
        {
            PresentationPart presentationPart1 = document.AddPresentationPart();
            GeneratePresentationPartContent(presentationPart1);

            // 加上通用的信息
            presentationPart1.GeneratePresentationPropertiesPart();
            presentationPart1.GenerateViewPropertiesPart();

            // 加上默认主题
            ThemePart defaultThemePart = presentationPart1.AddNewThemePart();
            _ = defaultThemePart; // 仅仅只是为了解决警告有变量没有被使用

            TableStylesPart defaultTableStylesPart = presentationPart1.GenerateTableStylesPart();
            _ = defaultTableStylesPart; // 仅仅只是为了解决警告有变量没有被使用

            return presentationPart1;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="presentationPart1"></param>
        private static void GeneratePresentationPartContent(PresentationPart presentationPart1)
        {
            Presentation presentation1 = new Presentation() { SaveSubsetFonts = true };
            presentation1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            presentation1.AddNamespaceDeclaration("r",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            presentation1.AddNamespaceDeclaration("p", "http://schemas.openxmlformats.org/presentationml/2006/main");

            // 12192000 EMU = 1280 Pixel
            // 6858000  EMU = 720  Pixel
            SlideSize slideSize1 = new SlideSize() { Cx = 12192000, Cy = 6858000 };
            NotesSize notesSize1 = new NotesSize() { Cx = 6858000L, Cy = 9144000L };

            DefaultTextStyle defaultTextStyle1 = new DefaultTextStyle();

            A.DefaultParagraphProperties defaultParagraphProperties1 = new A.DefaultParagraphProperties();
            A.DefaultRunProperties defaultRunProperties1 = new A.DefaultRunProperties() { Language = "zh-CN" };

            defaultParagraphProperties1.Append(defaultRunProperties1);

            A.Level1ParagraphProperties level1ParagraphProperties1 = new A.Level1ParagraphProperties()
            {
                LeftMargin = 0,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties2 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill1 = new A.SolidFill();
            A.SchemeColor schemeColor1 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill1.Append(schemeColor1);
            A.LatinFont latinFont1 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont1 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont1 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties2.Append(solidFill1);
            defaultRunProperties2.Append(latinFont1);
            defaultRunProperties2.Append(eastAsianFont1);
            defaultRunProperties2.Append(complexScriptFont1);

            level1ParagraphProperties1.Append(defaultRunProperties2);

            A.Level2ParagraphProperties level2ParagraphProperties1 = new A.Level2ParagraphProperties()
            {
                LeftMargin = 457200,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties3 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill2 = new A.SolidFill();
            A.SchemeColor schemeColor2 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill2.Append(schemeColor2);
            A.LatinFont latinFont2 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont2 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont2 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties3.Append(solidFill2);
            defaultRunProperties3.Append(latinFont2);
            defaultRunProperties3.Append(eastAsianFont2);
            defaultRunProperties3.Append(complexScriptFont2);

            level2ParagraphProperties1.Append(defaultRunProperties3);

            A.Level3ParagraphProperties level3ParagraphProperties1 = new A.Level3ParagraphProperties()
            {
                LeftMargin = 914400,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties4 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill3 = new A.SolidFill();
            A.SchemeColor schemeColor3 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill3.Append(schemeColor3);
            A.LatinFont latinFont3 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont3 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont3 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties4.Append(solidFill3);
            defaultRunProperties4.Append(latinFont3);
            defaultRunProperties4.Append(eastAsianFont3);
            defaultRunProperties4.Append(complexScriptFont3);

            level3ParagraphProperties1.Append(defaultRunProperties4);

            A.Level4ParagraphProperties level4ParagraphProperties1 = new A.Level4ParagraphProperties()
            {
                LeftMargin = 1371600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties5 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill4 = new A.SolidFill();
            A.SchemeColor schemeColor4 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill4.Append(schemeColor4);
            A.LatinFont latinFont4 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont4 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont4 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties5.Append(solidFill4);
            defaultRunProperties5.Append(latinFont4);
            defaultRunProperties5.Append(eastAsianFont4);
            defaultRunProperties5.Append(complexScriptFont4);

            level4ParagraphProperties1.Append(defaultRunProperties5);

            A.Level5ParagraphProperties level5ParagraphProperties1 = new A.Level5ParagraphProperties()
            {
                LeftMargin = 1828800,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties6 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill5 = new A.SolidFill();
            A.SchemeColor schemeColor5 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill5.Append(schemeColor5);
            A.LatinFont latinFont5 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont5 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont5 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties6.Append(solidFill5);
            defaultRunProperties6.Append(latinFont5);
            defaultRunProperties6.Append(eastAsianFont5);
            defaultRunProperties6.Append(complexScriptFont5);

            level5ParagraphProperties1.Append(defaultRunProperties6);

            A.Level6ParagraphProperties level6ParagraphProperties1 = new A.Level6ParagraphProperties()
            {
                LeftMargin = 2286000,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties7 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill6 = new A.SolidFill();
            A.SchemeColor schemeColor6 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill6.Append(schemeColor6);
            A.LatinFont latinFont6 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont6 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont6 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties7.Append(solidFill6);
            defaultRunProperties7.Append(latinFont6);
            defaultRunProperties7.Append(eastAsianFont6);
            defaultRunProperties7.Append(complexScriptFont6);

            level6ParagraphProperties1.Append(defaultRunProperties7);

            A.Level7ParagraphProperties level7ParagraphProperties1 = new A.Level7ParagraphProperties()
            {
                LeftMargin = 2743200,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties8 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill7 = new A.SolidFill();
            A.SchemeColor schemeColor7 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill7.Append(schemeColor7);
            A.LatinFont latinFont7 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont7 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont7 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties8.Append(solidFill7);
            defaultRunProperties8.Append(latinFont7);
            defaultRunProperties8.Append(eastAsianFont7);
            defaultRunProperties8.Append(complexScriptFont7);

            level7ParagraphProperties1.Append(defaultRunProperties8);

            A.Level8ParagraphProperties level8ParagraphProperties1 = new A.Level8ParagraphProperties()
            {
                LeftMargin = 3200400,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties9 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill8 = new A.SolidFill();
            A.SchemeColor schemeColor8 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill8.Append(schemeColor8);
            A.LatinFont latinFont8 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont8 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont8 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties9.Append(solidFill8);
            defaultRunProperties9.Append(latinFont8);
            defaultRunProperties9.Append(eastAsianFont8);
            defaultRunProperties9.Append(complexScriptFont8);

            level8ParagraphProperties1.Append(defaultRunProperties9);

            A.Level9ParagraphProperties level9ParagraphProperties1 = new A.Level9ParagraphProperties()
            {
                LeftMargin = 3657600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties10 =
                new A.DefaultRunProperties() { FontSize = 1800, Kerning = 1200 };

            A.SolidFill solidFill9 = new A.SolidFill();
            A.SchemeColor schemeColor9 = new A.SchemeColor() { Val = A.SchemeColorValues.Text1 };

            solidFill9.Append(schemeColor9);
            A.LatinFont latinFont9 = new A.LatinFont() { Typeface = "+mn-lt" };
            A.EastAsianFont eastAsianFont9 = new A.EastAsianFont() { Typeface = "+mn-ea" };
            A.ComplexScriptFont complexScriptFont9 = new A.ComplexScriptFont() { Typeface = "+mn-cs" };

            defaultRunProperties10.Append(solidFill9);
            defaultRunProperties10.Append(latinFont9);
            defaultRunProperties10.Append(eastAsianFont9);
            defaultRunProperties10.Append(complexScriptFont9);

            level9ParagraphProperties1.Append(defaultRunProperties10);

            defaultTextStyle1.Append(defaultParagraphProperties1);
            defaultTextStyle1.Append(level1ParagraphProperties1);
            defaultTextStyle1.Append(level2ParagraphProperties1);
            defaultTextStyle1.Append(level3ParagraphProperties1);
            defaultTextStyle1.Append(level4ParagraphProperties1);
            defaultTextStyle1.Append(level5ParagraphProperties1);
            defaultTextStyle1.Append(level6ParagraphProperties1);
            defaultTextStyle1.Append(level7ParagraphProperties1);
            defaultTextStyle1.Append(level8ParagraphProperties1);
            defaultTextStyle1.Append(level9ParagraphProperties1);

            PresentationExtensionList presentationExtensionList1 = new PresentationExtensionList();

            PresentationExtension presentationExtension1 =
                new PresentationExtension() { Uri = "{EFAFB233-063F-42B5-8137-9DF3F51BA10A}" };

            P15.SlideGuideList slideGuideList1 = new P15.SlideGuideList();
            slideGuideList1.AddNamespaceDeclaration("p15", "http://schemas.microsoft.com/office/powerpoint/2012/main");

            presentationExtension1.Append(slideGuideList1);

            presentationExtensionList1.Append(presentationExtension1);

            presentation1.Append
            (
                new SlideMasterIdList(),
                new SlideIdList(),
                slideSize1,
                notesSize1,
                defaultTextStyle1,
                presentationExtensionList1
            );

            presentationPart1.Presentation = presentation1;
        }
    }
}
