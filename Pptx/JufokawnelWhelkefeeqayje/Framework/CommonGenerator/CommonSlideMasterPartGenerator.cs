using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Flatten.Framework;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P14 = DocumentFormat.OpenXml.Office2010.PowerPoint;
using ApplicationNonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.ApplicationNonVisualDrawingProperties;
using ColorMap = DocumentFormat.OpenXml.Presentation.ColorMap;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualGroupShapeDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualGroupShapeDrawingProperties;
using NonVisualGroupShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualGroupShapeProperties;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonSlideMasterPartGenerator
    {
        public static SlideMasterPart AddNewSlideMasterPart(this PresentationPart presentationPart)
        {
            var (slideMasterPart, id) =
                presentationPart.AddNewPartWithGenerateId<SlideMasterPart>();

            var slideMasterIdList = presentationPart.Presentation.GetOrCreateElement<SlideMasterIdList>();
            // 页面模版要求 Id 大于 2147483647 = int.MaxValue
            SlideMasterId slideMasterId = new SlideMasterId()
            {
                Id = (uint) slideMasterIdList.Count() + int.MaxValue + 1, RelationshipId = id
            };
            slideMasterIdList.AppendChild(slideMasterId);

            GenerateSlideMasterPartContent(slideMasterPart);

            return slideMasterPart;
        }

        /// <summary>
        /// 生成空白的 SlideMasterPart 内容，此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="slideMasterPart1"></param>
        // Generates content of slideMasterPart1.
        private static void GenerateSlideMasterPartContent(SlideMasterPart slideMasterPart1)
        {
            SlideMaster slideMaster1 = new SlideMaster();
            slideMaster1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            slideMaster1.AddNamespaceDeclaration("r",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            slideMaster1.AddNamespaceDeclaration("p", "http://schemas.openxmlformats.org/presentationml/2006/main");

            CommonSlideData commonSlideData3 = new CommonSlideData();

            Background background1 = new Background();

            BackgroundStyleReference backgroundStyleReference1 =
                new BackgroundStyleReference() {Index = (UInt32Value) 1001U};
            A.SchemeColor schemeColor10 = new A.SchemeColor() {Val = A.SchemeColorValues.Background1};

            backgroundStyleReference1.Append(schemeColor10);

            background1.Append(backgroundStyleReference1);

            ShapeTree shapeTree3 = new ShapeTree();

            NonVisualGroupShapeProperties nonVisualGroupShapeProperties3 = new NonVisualGroupShapeProperties();
            NonVisualDrawingProperties nonVisualDrawingProperties4 =
                new NonVisualDrawingProperties() {Id = (UInt32Value) 1U, Name = ""};
            NonVisualGroupShapeDrawingProperties nonVisualGroupShapeDrawingProperties3 =
                new NonVisualGroupShapeDrawingProperties();
            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties4 =
                new ApplicationNonVisualDrawingProperties();

            nonVisualGroupShapeProperties3.Append(nonVisualDrawingProperties4);
            nonVisualGroupShapeProperties3.Append(nonVisualGroupShapeDrawingProperties3);
            nonVisualGroupShapeProperties3.Append(applicationNonVisualDrawingProperties4);

            GroupShapeProperties groupShapeProperties3 = new GroupShapeProperties();

            A.TransformGroup transformGroup3 = new A.TransformGroup();
            A.Offset offset4 = new A.Offset() {X = 0L, Y = 0L};
            A.Extents extents4 = new A.Extents() {Cx = 0L, Cy = 0L};
            A.ChildOffset childOffset3 = new A.ChildOffset() {X = 0L, Y = 0L};
            A.ChildExtents childExtents3 = new A.ChildExtents() {Cx = 0L, Cy = 0L};

            transformGroup3.Append(offset4);
            transformGroup3.Append(extents4);
            transformGroup3.Append(childOffset3);
            transformGroup3.Append(childExtents3);

            groupShapeProperties3.Append(transformGroup3);

            shapeTree3.Append(nonVisualGroupShapeProperties3);
            shapeTree3.Append(groupShapeProperties3);

            CommonSlideDataExtensionList commonSlideDataExtensionList3 = new CommonSlideDataExtensionList();

            CommonSlideDataExtension commonSlideDataExtension3 =
                new CommonSlideDataExtension() {Uri = "{BB962C8B-B14F-4D97-AF65-F5344CB8AC3E}"};

            P14.CreationId creationId3 = new P14.CreationId() {Val = (UInt32Value) 1333735316U};
            creationId3.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

            commonSlideDataExtension3.Append(creationId3);

            commonSlideDataExtensionList3.Append(commonSlideDataExtension3);

            commonSlideData3.Append(background1);
            commonSlideData3.Append(shapeTree3);
            commonSlideData3.Append(commonSlideDataExtensionList3);
            ColorMap colorMap1 = new ColorMap()
            {
                Background1 = A.ColorSchemeIndexValues.Light1,
                Text1 = A.ColorSchemeIndexValues.Dark1,
                Background2 = A.ColorSchemeIndexValues.Light2,
                Text2 = A.ColorSchemeIndexValues.Dark2,
                Accent1 = A.ColorSchemeIndexValues.Accent1,
                Accent2 = A.ColorSchemeIndexValues.Accent2,
                Accent3 = A.ColorSchemeIndexValues.Accent3,
                Accent4 = A.ColorSchemeIndexValues.Accent4,
                Accent5 = A.ColorSchemeIndexValues.Accent5,
                Accent6 = A.ColorSchemeIndexValues.Accent6,
                Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink
            };

            SlideLayoutIdList slideLayoutIdList1 = new SlideLayoutIdList();

            TextStyles textStyles1 = new TextStyles();

            TitleStyle titleStyle1 = new TitleStyle();

            A.Level1ParagraphProperties level1ParagraphProperties2 = new A.Level1ParagraphProperties()
            {
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing1 = new A.LineSpacing();
            A.SpacingPercent spacingPercent1 = new A.SpacingPercent() {Val = 90000};

            lineSpacing1.Append(spacingPercent1);

            A.SpaceBefore spaceBefore1 = new A.SpaceBefore();
            A.SpacingPercent spacingPercent2 = new A.SpacingPercent() {Val = 0};

            spaceBefore1.Append(spacingPercent2);
            A.NoBullet noBullet1 = new A.NoBullet();

            A.DefaultRunProperties defaultRunProperties11 =
                new A.DefaultRunProperties() {FontSize = 4400, Kerning = 1200};

            A.SolidFill solidFill10 = new A.SolidFill();
            A.SchemeColor schemeColor11 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill10.Append(schemeColor11);
            A.LatinFont latinFont10 = new A.LatinFont() {Typeface = "+mj-lt"};
            A.EastAsianFont eastAsianFont10 = new A.EastAsianFont() {Typeface = "+mj-ea"};
            A.ComplexScriptFont complexScriptFont10 = new A.ComplexScriptFont() {Typeface = "+mj-cs"};

            defaultRunProperties11.Append(solidFill10);
            defaultRunProperties11.Append(latinFont10);
            defaultRunProperties11.Append(eastAsianFont10);
            defaultRunProperties11.Append(complexScriptFont10);

            level1ParagraphProperties2.Append(lineSpacing1);
            level1ParagraphProperties2.Append(spaceBefore1);
            level1ParagraphProperties2.Append(noBullet1);
            level1ParagraphProperties2.Append(defaultRunProperties11);

            titleStyle1.Append(level1ParagraphProperties2);

            BodyStyle bodyStyle1 = new BodyStyle();

            A.Level1ParagraphProperties level1ParagraphProperties3 = new A.Level1ParagraphProperties()
            {
                LeftMargin = 228600,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing2 = new A.LineSpacing();
            A.SpacingPercent spacingPercent3 = new A.SpacingPercent() {Val = 90000};

            lineSpacing2.Append(spacingPercent3);

            A.SpaceBefore spaceBefore2 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints1 = new A.SpacingPoints() {Val = 1000};

            spaceBefore2.Append(spacingPoints1);
            A.BulletFont bulletFont1 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet1 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties12 =
                new A.DefaultRunProperties() {FontSize = 2800, Kerning = 1200};

            A.SolidFill solidFill11 = new A.SolidFill();
            A.SchemeColor schemeColor12 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill11.Append(schemeColor12);
            A.LatinFont latinFont11 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont11 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont11 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties12.Append(solidFill11);
            defaultRunProperties12.Append(latinFont11);
            defaultRunProperties12.Append(eastAsianFont11);
            defaultRunProperties12.Append(complexScriptFont11);

            level1ParagraphProperties3.Append(lineSpacing2);
            level1ParagraphProperties3.Append(spaceBefore2);
            level1ParagraphProperties3.Append(bulletFont1);
            level1ParagraphProperties3.Append(characterBullet1);
            level1ParagraphProperties3.Append(defaultRunProperties12);

            A.Level2ParagraphProperties level2ParagraphProperties2 = new A.Level2ParagraphProperties()
            {
                LeftMargin = 685800,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing3 = new A.LineSpacing();
            A.SpacingPercent spacingPercent4 = new A.SpacingPercent() {Val = 90000};

            lineSpacing3.Append(spacingPercent4);

            A.SpaceBefore spaceBefore3 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints2 = new A.SpacingPoints() {Val = 500};

            spaceBefore3.Append(spacingPoints2);
            A.BulletFont bulletFont2 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet2 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties13 =
                new A.DefaultRunProperties() {FontSize = 2400, Kerning = 1200};

            A.SolidFill solidFill12 = new A.SolidFill();
            A.SchemeColor schemeColor13 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill12.Append(schemeColor13);
            A.LatinFont latinFont12 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont12 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont12 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties13.Append(solidFill12);
            defaultRunProperties13.Append(latinFont12);
            defaultRunProperties13.Append(eastAsianFont12);
            defaultRunProperties13.Append(complexScriptFont12);

            level2ParagraphProperties2.Append(lineSpacing3);
            level2ParagraphProperties2.Append(spaceBefore3);
            level2ParagraphProperties2.Append(bulletFont2);
            level2ParagraphProperties2.Append(characterBullet2);
            level2ParagraphProperties2.Append(defaultRunProperties13);

            A.Level3ParagraphProperties level3ParagraphProperties2 = new A.Level3ParagraphProperties()
            {
                LeftMargin = 1143000,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing4 = new A.LineSpacing();
            A.SpacingPercent spacingPercent5 = new A.SpacingPercent() {Val = 90000};

            lineSpacing4.Append(spacingPercent5);

            A.SpaceBefore spaceBefore4 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints3 = new A.SpacingPoints() {Val = 500};

            spaceBefore4.Append(spacingPoints3);
            A.BulletFont bulletFont3 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet3 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties14 =
                new A.DefaultRunProperties() {FontSize = 2000, Kerning = 1200};

            A.SolidFill solidFill13 = new A.SolidFill();
            A.SchemeColor schemeColor14 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill13.Append(schemeColor14);
            A.LatinFont latinFont13 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont13 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont13 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties14.Append(solidFill13);
            defaultRunProperties14.Append(latinFont13);
            defaultRunProperties14.Append(eastAsianFont13);
            defaultRunProperties14.Append(complexScriptFont13);

            level3ParagraphProperties2.Append(lineSpacing4);
            level3ParagraphProperties2.Append(spaceBefore4);
            level3ParagraphProperties2.Append(bulletFont3);
            level3ParagraphProperties2.Append(characterBullet3);
            level3ParagraphProperties2.Append(defaultRunProperties14);

            A.Level4ParagraphProperties level4ParagraphProperties2 = new A.Level4ParagraphProperties()
            {
                LeftMargin = 1600200,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing5 = new A.LineSpacing();
            A.SpacingPercent spacingPercent6 = new A.SpacingPercent() {Val = 90000};

            lineSpacing5.Append(spacingPercent6);

            A.SpaceBefore spaceBefore5 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints4 = new A.SpacingPoints() {Val = 500};

            spaceBefore5.Append(spacingPoints4);
            A.BulletFont bulletFont4 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet4 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties15 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill14 = new A.SolidFill();
            A.SchemeColor schemeColor15 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill14.Append(schemeColor15);
            A.LatinFont latinFont14 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont14 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont14 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties15.Append(solidFill14);
            defaultRunProperties15.Append(latinFont14);
            defaultRunProperties15.Append(eastAsianFont14);
            defaultRunProperties15.Append(complexScriptFont14);

            level4ParagraphProperties2.Append(lineSpacing5);
            level4ParagraphProperties2.Append(spaceBefore5);
            level4ParagraphProperties2.Append(bulletFont4);
            level4ParagraphProperties2.Append(characterBullet4);
            level4ParagraphProperties2.Append(defaultRunProperties15);

            A.Level5ParagraphProperties level5ParagraphProperties2 = new A.Level5ParagraphProperties()
            {
                LeftMargin = 2057400,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing6 = new A.LineSpacing();
            A.SpacingPercent spacingPercent7 = new A.SpacingPercent() {Val = 90000};

            lineSpacing6.Append(spacingPercent7);

            A.SpaceBefore spaceBefore6 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints5 = new A.SpacingPoints() {Val = 500};

            spaceBefore6.Append(spacingPoints5);
            A.BulletFont bulletFont5 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet5 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties16 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill15 = new A.SolidFill();
            A.SchemeColor schemeColor16 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill15.Append(schemeColor16);
            A.LatinFont latinFont15 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont15 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont15 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties16.Append(solidFill15);
            defaultRunProperties16.Append(latinFont15);
            defaultRunProperties16.Append(eastAsianFont15);
            defaultRunProperties16.Append(complexScriptFont15);

            level5ParagraphProperties2.Append(lineSpacing6);
            level5ParagraphProperties2.Append(spaceBefore6);
            level5ParagraphProperties2.Append(bulletFont5);
            level5ParagraphProperties2.Append(characterBullet5);
            level5ParagraphProperties2.Append(defaultRunProperties16);

            A.Level6ParagraphProperties level6ParagraphProperties2 = new A.Level6ParagraphProperties()
            {
                LeftMargin = 2514600,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing7 = new A.LineSpacing();
            A.SpacingPercent spacingPercent8 = new A.SpacingPercent() {Val = 90000};

            lineSpacing7.Append(spacingPercent8);

            A.SpaceBefore spaceBefore7 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints6 = new A.SpacingPoints() {Val = 500};

            spaceBefore7.Append(spacingPoints6);
            A.BulletFont bulletFont6 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet6 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties17 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill16 = new A.SolidFill();
            A.SchemeColor schemeColor17 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill16.Append(schemeColor17);
            A.LatinFont latinFont16 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont16 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont16 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties17.Append(solidFill16);
            defaultRunProperties17.Append(latinFont16);
            defaultRunProperties17.Append(eastAsianFont16);
            defaultRunProperties17.Append(complexScriptFont16);

            level6ParagraphProperties2.Append(lineSpacing7);
            level6ParagraphProperties2.Append(spaceBefore7);
            level6ParagraphProperties2.Append(bulletFont6);
            level6ParagraphProperties2.Append(characterBullet6);
            level6ParagraphProperties2.Append(defaultRunProperties17);

            A.Level7ParagraphProperties level7ParagraphProperties2 = new A.Level7ParagraphProperties()
            {
                LeftMargin = 2971800,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing8 = new A.LineSpacing();
            A.SpacingPercent spacingPercent9 = new A.SpacingPercent() {Val = 90000};

            lineSpacing8.Append(spacingPercent9);

            A.SpaceBefore spaceBefore8 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints7 = new A.SpacingPoints() {Val = 500};

            spaceBefore8.Append(spacingPoints7);
            A.BulletFont bulletFont7 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet7 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties18 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill17 = new A.SolidFill();
            A.SchemeColor schemeColor18 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill17.Append(schemeColor18);
            A.LatinFont latinFont17 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont17 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont17 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties18.Append(solidFill17);
            defaultRunProperties18.Append(latinFont17);
            defaultRunProperties18.Append(eastAsianFont17);
            defaultRunProperties18.Append(complexScriptFont17);

            level7ParagraphProperties2.Append(lineSpacing8);
            level7ParagraphProperties2.Append(spaceBefore8);
            level7ParagraphProperties2.Append(bulletFont7);
            level7ParagraphProperties2.Append(characterBullet7);
            level7ParagraphProperties2.Append(defaultRunProperties18);

            A.Level8ParagraphProperties level8ParagraphProperties2 = new A.Level8ParagraphProperties()
            {
                LeftMargin = 3429000,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing9 = new A.LineSpacing();
            A.SpacingPercent spacingPercent10 = new A.SpacingPercent() {Val = 90000};

            lineSpacing9.Append(spacingPercent10);

            A.SpaceBefore spaceBefore9 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints8 = new A.SpacingPoints() {Val = 500};

            spaceBefore9.Append(spacingPoints8);
            A.BulletFont bulletFont8 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet8 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties19 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill18 = new A.SolidFill();
            A.SchemeColor schemeColor19 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill18.Append(schemeColor19);
            A.LatinFont latinFont18 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont18 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont18 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties19.Append(solidFill18);
            defaultRunProperties19.Append(latinFont18);
            defaultRunProperties19.Append(eastAsianFont18);
            defaultRunProperties19.Append(complexScriptFont18);

            level8ParagraphProperties2.Append(lineSpacing9);
            level8ParagraphProperties2.Append(spaceBefore9);
            level8ParagraphProperties2.Append(bulletFont8);
            level8ParagraphProperties2.Append(characterBullet8);
            level8ParagraphProperties2.Append(defaultRunProperties19);

            A.Level9ParagraphProperties level9ParagraphProperties2 = new A.Level9ParagraphProperties()
            {
                LeftMargin = 3886200,
                Indent = -228600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.LineSpacing lineSpacing10 = new A.LineSpacing();
            A.SpacingPercent spacingPercent11 = new A.SpacingPercent() {Val = 90000};

            lineSpacing10.Append(spacingPercent11);

            A.SpaceBefore spaceBefore10 = new A.SpaceBefore();
            A.SpacingPoints spacingPoints9 = new A.SpacingPoints() {Val = 500};

            spaceBefore10.Append(spacingPoints9);
            A.BulletFont bulletFont9 = new A.BulletFont()
            {
                Typeface = "Arial", Panose = "020B0604020202020204", PitchFamily = 34, CharacterSet = 0
            };
            A.CharacterBullet characterBullet9 = new A.CharacterBullet() {Char = "•"};

            A.DefaultRunProperties defaultRunProperties20 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill19 = new A.SolidFill();
            A.SchemeColor schemeColor20 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill19.Append(schemeColor20);
            A.LatinFont latinFont19 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont19 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont19 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties20.Append(solidFill19);
            defaultRunProperties20.Append(latinFont19);
            defaultRunProperties20.Append(eastAsianFont19);
            defaultRunProperties20.Append(complexScriptFont19);

            level9ParagraphProperties2.Append(lineSpacing10);
            level9ParagraphProperties2.Append(spaceBefore10);
            level9ParagraphProperties2.Append(bulletFont9);
            level9ParagraphProperties2.Append(characterBullet9);
            level9ParagraphProperties2.Append(defaultRunProperties20);

            bodyStyle1.Append(level1ParagraphProperties3);
            bodyStyle1.Append(level2ParagraphProperties2);
            bodyStyle1.Append(level3ParagraphProperties2);
            bodyStyle1.Append(level4ParagraphProperties2);
            bodyStyle1.Append(level5ParagraphProperties2);
            bodyStyle1.Append(level6ParagraphProperties2);
            bodyStyle1.Append(level7ParagraphProperties2);
            bodyStyle1.Append(level8ParagraphProperties2);
            bodyStyle1.Append(level9ParagraphProperties2);

            OtherStyle otherStyle1 = new OtherStyle();

            A.DefaultParagraphProperties defaultParagraphProperties2 = new A.DefaultParagraphProperties();
            A.DefaultRunProperties defaultRunProperties21 = new A.DefaultRunProperties() {Language = "zh-CN"};

            defaultParagraphProperties2.Append(defaultRunProperties21);

            A.Level1ParagraphProperties level1ParagraphProperties4 = new A.Level1ParagraphProperties()
            {
                LeftMargin = 0,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties22 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill20 = new A.SolidFill();
            A.SchemeColor schemeColor21 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill20.Append(schemeColor21);
            A.LatinFont latinFont20 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont20 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont20 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties22.Append(solidFill20);
            defaultRunProperties22.Append(latinFont20);
            defaultRunProperties22.Append(eastAsianFont20);
            defaultRunProperties22.Append(complexScriptFont20);

            level1ParagraphProperties4.Append(defaultRunProperties22);

            A.Level2ParagraphProperties level2ParagraphProperties3 = new A.Level2ParagraphProperties()
            {
                LeftMargin = 457200,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties23 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill21 = new A.SolidFill();
            A.SchemeColor schemeColor22 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill21.Append(schemeColor22);
            A.LatinFont latinFont21 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont21 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont21 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties23.Append(solidFill21);
            defaultRunProperties23.Append(latinFont21);
            defaultRunProperties23.Append(eastAsianFont21);
            defaultRunProperties23.Append(complexScriptFont21);

            level2ParagraphProperties3.Append(defaultRunProperties23);

            A.Level3ParagraphProperties level3ParagraphProperties3 = new A.Level3ParagraphProperties()
            {
                LeftMargin = 914400,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties24 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill22 = new A.SolidFill();
            A.SchemeColor schemeColor23 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill22.Append(schemeColor23);
            A.LatinFont latinFont22 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont22 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont22 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties24.Append(solidFill22);
            defaultRunProperties24.Append(latinFont22);
            defaultRunProperties24.Append(eastAsianFont22);
            defaultRunProperties24.Append(complexScriptFont22);

            level3ParagraphProperties3.Append(defaultRunProperties24);

            A.Level4ParagraphProperties level4ParagraphProperties3 = new A.Level4ParagraphProperties()
            {
                LeftMargin = 1371600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties25 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill23 = new A.SolidFill();
            A.SchemeColor schemeColor24 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill23.Append(schemeColor24);
            A.LatinFont latinFont23 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont23 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont23 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties25.Append(solidFill23);
            defaultRunProperties25.Append(latinFont23);
            defaultRunProperties25.Append(eastAsianFont23);
            defaultRunProperties25.Append(complexScriptFont23);

            level4ParagraphProperties3.Append(defaultRunProperties25);

            A.Level5ParagraphProperties level5ParagraphProperties3 = new A.Level5ParagraphProperties()
            {
                LeftMargin = 1828800,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties26 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill24 = new A.SolidFill();
            A.SchemeColor schemeColor25 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill24.Append(schemeColor25);
            A.LatinFont latinFont24 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont24 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont24 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties26.Append(solidFill24);
            defaultRunProperties26.Append(latinFont24);
            defaultRunProperties26.Append(eastAsianFont24);
            defaultRunProperties26.Append(complexScriptFont24);

            level5ParagraphProperties3.Append(defaultRunProperties26);

            A.Level6ParagraphProperties level6ParagraphProperties3 = new A.Level6ParagraphProperties()
            {
                LeftMargin = 2286000,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties27 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill25 = new A.SolidFill();
            A.SchemeColor schemeColor26 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill25.Append(schemeColor26);
            A.LatinFont latinFont25 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont25 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont25 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties27.Append(solidFill25);
            defaultRunProperties27.Append(latinFont25);
            defaultRunProperties27.Append(eastAsianFont25);
            defaultRunProperties27.Append(complexScriptFont25);

            level6ParagraphProperties3.Append(defaultRunProperties27);

            A.Level7ParagraphProperties level7ParagraphProperties3 = new A.Level7ParagraphProperties()
            {
                LeftMargin = 2743200,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties28 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill26 = new A.SolidFill();
            A.SchemeColor schemeColor27 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill26.Append(schemeColor27);
            A.LatinFont latinFont26 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont26 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont26 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties28.Append(solidFill26);
            defaultRunProperties28.Append(latinFont26);
            defaultRunProperties28.Append(eastAsianFont26);
            defaultRunProperties28.Append(complexScriptFont26);

            level7ParagraphProperties3.Append(defaultRunProperties28);

            A.Level8ParagraphProperties level8ParagraphProperties3 = new A.Level8ParagraphProperties()
            {
                LeftMargin = 3200400,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties29 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill27 = new A.SolidFill();
            A.SchemeColor schemeColor28 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill27.Append(schemeColor28);
            A.LatinFont latinFont27 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont27 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont27 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties29.Append(solidFill27);
            defaultRunProperties29.Append(latinFont27);
            defaultRunProperties29.Append(eastAsianFont27);
            defaultRunProperties29.Append(complexScriptFont27);

            level8ParagraphProperties3.Append(defaultRunProperties29);

            A.Level9ParagraphProperties level9ParagraphProperties3 = new A.Level9ParagraphProperties()
            {
                LeftMargin = 3657600,
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            };

            A.DefaultRunProperties defaultRunProperties30 =
                new A.DefaultRunProperties() {FontSize = 1800, Kerning = 1200};

            A.SolidFill solidFill28 = new A.SolidFill();
            A.SchemeColor schemeColor29 = new A.SchemeColor() {Val = A.SchemeColorValues.Text1};

            solidFill28.Append(schemeColor29);
            A.LatinFont latinFont28 = new A.LatinFont() {Typeface = "+mn-lt"};
            A.EastAsianFont eastAsianFont28 = new A.EastAsianFont() {Typeface = "+mn-ea"};
            A.ComplexScriptFont complexScriptFont28 = new A.ComplexScriptFont() {Typeface = "+mn-cs"};

            defaultRunProperties30.Append(solidFill28);
            defaultRunProperties30.Append(latinFont28);
            defaultRunProperties30.Append(eastAsianFont28);
            defaultRunProperties30.Append(complexScriptFont28);

            level9ParagraphProperties3.Append(defaultRunProperties30);

            otherStyle1.Append(defaultParagraphProperties2);
            otherStyle1.Append(level1ParagraphProperties4);
            otherStyle1.Append(level2ParagraphProperties3);
            otherStyle1.Append(level3ParagraphProperties3);
            otherStyle1.Append(level4ParagraphProperties3);
            otherStyle1.Append(level5ParagraphProperties3);
            otherStyle1.Append(level6ParagraphProperties3);
            otherStyle1.Append(level7ParagraphProperties3);
            otherStyle1.Append(level8ParagraphProperties3);
            otherStyle1.Append(level9ParagraphProperties3);

            textStyles1.Append(titleStyle1);
            textStyles1.Append(bodyStyle1);
            textStyles1.Append(otherStyle1);

            slideMaster1.Append(commonSlideData3);
            slideMaster1.Append(colorMap1);
            slideMaster1.Append(slideLayoutIdList1);
            slideMaster1.Append(textStyles1);

            slideMasterPart1.SlideMaster = slideMaster1;
        }
    }
}
