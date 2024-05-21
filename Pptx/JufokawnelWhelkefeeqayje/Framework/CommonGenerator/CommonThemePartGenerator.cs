using DocumentFormat.OpenXml.Packaging;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonThemePartGenerator
    {
        public static ThemePart AddNewThemePart(this PresentationPart presentationPart)
        {
            var (themePart, _) = presentationPart.AddNewPartWithGenerateId<ThemePart>();
            GenerateThemePartContent(themePart);

            return themePart;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="themePart1"></param>
        // Generates content of themePart1.
        private static void GenerateThemePartContent(ThemePart themePart1)
        {
            DocumentFormat.OpenXml.Drawing.Theme theme1 =
                new DocumentFormat.OpenXml.Drawing.Theme() { Name = "Office 主题​​" };
            theme1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

            DocumentFormat.OpenXml.Drawing.ThemeElements themeElements1 =
                new DocumentFormat.OpenXml.Drawing.ThemeElements();

            DocumentFormat.OpenXml.Drawing.ColorScheme colorScheme1 =
                new DocumentFormat.OpenXml.Drawing.ColorScheme() { Name = "Office" };

            DocumentFormat.OpenXml.Drawing.Dark1Color dark1Color1 = new DocumentFormat.OpenXml.Drawing.Dark1Color();
            DocumentFormat.OpenXml.Drawing.SystemColor systemColor1 = new DocumentFormat.OpenXml.Drawing.SystemColor()
            {
                Val = DocumentFormat.OpenXml.Drawing.SystemColorValues.WindowText,
                LastColor = "000000"
            };

            dark1Color1.Append(systemColor1);

            DocumentFormat.OpenXml.Drawing.Light1Color light1Color1 = new DocumentFormat.OpenXml.Drawing.Light1Color();
            DocumentFormat.OpenXml.Drawing.SystemColor systemColor2 = new DocumentFormat.OpenXml.Drawing.SystemColor()
            {
                Val = DocumentFormat.OpenXml.Drawing.SystemColorValues.Window,
                LastColor = "FFFFFF"
            };

            light1Color1.Append(systemColor2);

            DocumentFormat.OpenXml.Drawing.Dark2Color dark2Color1 = new DocumentFormat.OpenXml.Drawing.Dark2Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex1 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "44546A" };

            dark2Color1.Append(rgbColorModelHex1);

            DocumentFormat.OpenXml.Drawing.Light2Color light2Color1 = new DocumentFormat.OpenXml.Drawing.Light2Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex2 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "E7E6E6" };

            light2Color1.Append(rgbColorModelHex2);

            DocumentFormat.OpenXml.Drawing.Accent1Color accent1Color1 =
                new DocumentFormat.OpenXml.Drawing.Accent1Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex3 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "5B9BD5" };

            accent1Color1.Append(rgbColorModelHex3);

            DocumentFormat.OpenXml.Drawing.Accent2Color accent2Color1 =
                new DocumentFormat.OpenXml.Drawing.Accent2Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex4 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "ED7D31" };

            accent2Color1.Append(rgbColorModelHex4);

            DocumentFormat.OpenXml.Drawing.Accent3Color accent3Color1 =
                new DocumentFormat.OpenXml.Drawing.Accent3Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex5 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "A5A5A5" };

            accent3Color1.Append(rgbColorModelHex5);

            DocumentFormat.OpenXml.Drawing.Accent4Color accent4Color1 =
                new DocumentFormat.OpenXml.Drawing.Accent4Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex6 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "FFC000" };

            accent4Color1.Append(rgbColorModelHex6);

            DocumentFormat.OpenXml.Drawing.Accent5Color accent5Color1 =
                new DocumentFormat.OpenXml.Drawing.Accent5Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex7 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "4472C4" };

            accent5Color1.Append(rgbColorModelHex7);

            DocumentFormat.OpenXml.Drawing.Accent6Color accent6Color1 =
                new DocumentFormat.OpenXml.Drawing.Accent6Color();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex8 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "70AD47" };

            accent6Color1.Append(rgbColorModelHex8);

            DocumentFormat.OpenXml.Drawing.Hyperlink hyperlink1 = new DocumentFormat.OpenXml.Drawing.Hyperlink();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex9 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "0563C1" };

            hyperlink1.Append(rgbColorModelHex9);

            DocumentFormat.OpenXml.Drawing.FollowedHyperlinkColor followedHyperlinkColor1 =
                new DocumentFormat.OpenXml.Drawing.FollowedHyperlinkColor();
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex10 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "954F72" };

            followedHyperlinkColor1.Append(rgbColorModelHex10);

            colorScheme1.Append(dark1Color1);
            colorScheme1.Append(light1Color1);
            colorScheme1.Append(dark2Color1);
            colorScheme1.Append(light2Color1);
            colorScheme1.Append(accent1Color1);
            colorScheme1.Append(accent2Color1);
            colorScheme1.Append(accent3Color1);
            colorScheme1.Append(accent4Color1);
            colorScheme1.Append(accent5Color1);
            colorScheme1.Append(accent6Color1);
            colorScheme1.Append(hyperlink1);
            colorScheme1.Append(followedHyperlinkColor1);

            DocumentFormat.OpenXml.Drawing.FontScheme fontScheme1 =
                new DocumentFormat.OpenXml.Drawing.FontScheme() { Name = "Office" };

            DocumentFormat.OpenXml.Drawing.MajorFont majorFont1 = new DocumentFormat.OpenXml.Drawing.MajorFont();
            DocumentFormat.OpenXml.Drawing.LatinFont latinFont29 =
                new DocumentFormat.OpenXml.Drawing.LatinFont() { Typeface = "等线 Light", Panose = "020F0302020204030204" };
            DocumentFormat.OpenXml.Drawing.EastAsianFont eastAsianFont29 =
                new DocumentFormat.OpenXml.Drawing.EastAsianFont() { Typeface = "" };
            DocumentFormat.OpenXml.Drawing.ComplexScriptFont complexScriptFont29 =
                new DocumentFormat.OpenXml.Drawing.ComplexScriptFont() { Typeface = "" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont1 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Jpan", Typeface = "游ゴシック Light" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont2 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hang", Typeface = "맑은 고딕" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont3 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hans", Typeface = "等线 Light" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont4 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hant", Typeface = "新細明體" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont5 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Arab", Typeface = "Times New Roman" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont6 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hebr", Typeface = "Times New Roman" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont7 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Thai", Typeface = "Angsana New" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont8 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Ethi", Typeface = "Nyala" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont9 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Beng", Typeface = "Vrinda" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont10 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Gujr", Typeface = "Shruti" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont11 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Khmr", Typeface = "MoolBoran" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont12 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Knda", Typeface = "Tunga" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont13 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Guru", Typeface = "Raavi" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont14 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Cans", Typeface = "Euphemia" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont15 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont()
                {
                    Script = "Cher",
                    Typeface = "Plantagenet Cherokee"
                };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont16 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont()
                {
                    Script = "Yiii",
                    Typeface = "Microsoft Yi Baiti"
                };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont17 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont()
                {
                    Script = "Tibt",
                    Typeface = "Microsoft Himalaya"
                };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont18 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Thaa", Typeface = "MV Boli" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont19 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Deva", Typeface = "Mangal" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont20 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Telu", Typeface = "Gautami" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont21 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Taml", Typeface = "Latha" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont22 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Syrc", Typeface = "Estrangelo Edessa" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont23 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Orya", Typeface = "Kalinga" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont24 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Mlym", Typeface = "Kartika" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont25 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Laoo", Typeface = "DokChampa" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont26 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Sinh", Typeface = "Iskoola Pota" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont27 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Mong", Typeface = "Mongolian Baiti" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont28 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Viet", Typeface = "Times New Roman" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont29 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Uigh", Typeface = "Microsoft Uighur" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont30 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Geor", Typeface = "Sylfaen" };

            majorFont1.Append(latinFont29);
            majorFont1.Append(eastAsianFont29);
            majorFont1.Append(complexScriptFont29);
            majorFont1.Append(supplementalFont1);
            majorFont1.Append(supplementalFont2);
            majorFont1.Append(supplementalFont3);
            majorFont1.Append(supplementalFont4);
            majorFont1.Append(supplementalFont5);
            majorFont1.Append(supplementalFont6);
            majorFont1.Append(supplementalFont7);
            majorFont1.Append(supplementalFont8);
            majorFont1.Append(supplementalFont9);
            majorFont1.Append(supplementalFont10);
            majorFont1.Append(supplementalFont11);
            majorFont1.Append(supplementalFont12);
            majorFont1.Append(supplementalFont13);
            majorFont1.Append(supplementalFont14);
            majorFont1.Append(supplementalFont15);
            majorFont1.Append(supplementalFont16);
            majorFont1.Append(supplementalFont17);
            majorFont1.Append(supplementalFont18);
            majorFont1.Append(supplementalFont19);
            majorFont1.Append(supplementalFont20);
            majorFont1.Append(supplementalFont21);
            majorFont1.Append(supplementalFont22);
            majorFont1.Append(supplementalFont23);
            majorFont1.Append(supplementalFont24);
            majorFont1.Append(supplementalFont25);
            majorFont1.Append(supplementalFont26);
            majorFont1.Append(supplementalFont27);
            majorFont1.Append(supplementalFont28);
            majorFont1.Append(supplementalFont29);
            majorFont1.Append(supplementalFont30);

            DocumentFormat.OpenXml.Drawing.MinorFont minorFont1 = new DocumentFormat.OpenXml.Drawing.MinorFont();
            DocumentFormat.OpenXml.Drawing.LatinFont latinFont30 =
                new DocumentFormat.OpenXml.Drawing.LatinFont() { Typeface = "等线", Panose = "020F0502020204030204" };
            DocumentFormat.OpenXml.Drawing.EastAsianFont eastAsianFont30 =
                new DocumentFormat.OpenXml.Drawing.EastAsianFont() { Typeface = "" };
            DocumentFormat.OpenXml.Drawing.ComplexScriptFont complexScriptFont30 =
                new DocumentFormat.OpenXml.Drawing.ComplexScriptFont() { Typeface = "" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont31 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Jpan", Typeface = "游ゴシック" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont32 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hang", Typeface = "맑은 고딕" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont33 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hans", Typeface = "等线" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont34 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hant", Typeface = "新細明體" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont35 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Arab", Typeface = "Arial" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont36 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Hebr", Typeface = "Arial" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont37 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Thai", Typeface = "Cordia New" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont38 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Ethi", Typeface = "Nyala" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont39 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Beng", Typeface = "Vrinda" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont40 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Gujr", Typeface = "Shruti" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont41 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Khmr", Typeface = "DaunPenh" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont42 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Knda", Typeface = "Tunga" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont43 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Guru", Typeface = "Raavi" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont44 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Cans", Typeface = "Euphemia" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont45 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont()
                {
                    Script = "Cher",
                    Typeface = "Plantagenet Cherokee"
                };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont46 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont()
                {
                    Script = "Yiii",
                    Typeface = "Microsoft Yi Baiti"
                };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont47 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont()
                {
                    Script = "Tibt",
                    Typeface = "Microsoft Himalaya"
                };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont48 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Thaa", Typeface = "MV Boli" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont49 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Deva", Typeface = "Mangal" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont50 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Telu", Typeface = "Gautami" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont51 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Taml", Typeface = "Latha" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont52 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Syrc", Typeface = "Estrangelo Edessa" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont53 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Orya", Typeface = "Kalinga" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont54 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Mlym", Typeface = "Kartika" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont55 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Laoo", Typeface = "DokChampa" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont56 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Sinh", Typeface = "Iskoola Pota" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont57 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Mong", Typeface = "Mongolian Baiti" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont58 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Viet", Typeface = "Arial" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont59 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Uigh", Typeface = "Microsoft Uighur" };
            DocumentFormat.OpenXml.Drawing.SupplementalFont supplementalFont60 =
                new DocumentFormat.OpenXml.Drawing.SupplementalFont() { Script = "Geor", Typeface = "Sylfaen" };

            minorFont1.Append(latinFont30);
            minorFont1.Append(eastAsianFont30);
            minorFont1.Append(complexScriptFont30);
            minorFont1.Append(supplementalFont31);
            minorFont1.Append(supplementalFont32);
            minorFont1.Append(supplementalFont33);
            minorFont1.Append(supplementalFont34);
            minorFont1.Append(supplementalFont35);
            minorFont1.Append(supplementalFont36);
            minorFont1.Append(supplementalFont37);
            minorFont1.Append(supplementalFont38);
            minorFont1.Append(supplementalFont39);
            minorFont1.Append(supplementalFont40);
            minorFont1.Append(supplementalFont41);
            minorFont1.Append(supplementalFont42);
            minorFont1.Append(supplementalFont43);
            minorFont1.Append(supplementalFont44);
            minorFont1.Append(supplementalFont45);
            minorFont1.Append(supplementalFont46);
            minorFont1.Append(supplementalFont47);
            minorFont1.Append(supplementalFont48);
            minorFont1.Append(supplementalFont49);
            minorFont1.Append(supplementalFont50);
            minorFont1.Append(supplementalFont51);
            minorFont1.Append(supplementalFont52);
            minorFont1.Append(supplementalFont53);
            minorFont1.Append(supplementalFont54);
            minorFont1.Append(supplementalFont55);
            minorFont1.Append(supplementalFont56);
            minorFont1.Append(supplementalFont57);
            minorFont1.Append(supplementalFont58);
            minorFont1.Append(supplementalFont59);
            minorFont1.Append(supplementalFont60);

            fontScheme1.Append(majorFont1);
            fontScheme1.Append(minorFont1);

            DocumentFormat.OpenXml.Drawing.FormatScheme formatScheme1 =
                new DocumentFormat.OpenXml.Drawing.FormatScheme() { Name = "Office" };

            DocumentFormat.OpenXml.Drawing.FillStyleList fillStyleList1 =
                new DocumentFormat.OpenXml.Drawing.FillStyleList();

            DocumentFormat.OpenXml.Drawing.SolidFill solidFill29 = new DocumentFormat.OpenXml.Drawing.SolidFill();
            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor30 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };

            solidFill29.Append(schemeColor30);

            DocumentFormat.OpenXml.Drawing.GradientFill gradientFill1 =
                new DocumentFormat.OpenXml.Drawing.GradientFill() { RotateWithShape = true };

            DocumentFormat.OpenXml.Drawing.GradientStopList gradientStopList1 =
                new DocumentFormat.OpenXml.Drawing.GradientStopList();

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop1 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 0 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor31 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation1 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 110000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation1 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 105000 };
            DocumentFormat.OpenXml.Drawing.Tint tint1 = new DocumentFormat.OpenXml.Drawing.Tint() { Val = 67000 };

            schemeColor31.Append(luminanceModulation1);
            schemeColor31.Append(saturationModulation1);
            schemeColor31.Append(tint1);

            gradientStop1.Append(schemeColor31);

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop2 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 50000 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor32 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation2 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 105000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation2 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 103000 };
            DocumentFormat.OpenXml.Drawing.Tint tint2 = new DocumentFormat.OpenXml.Drawing.Tint() { Val = 73000 };

            schemeColor32.Append(luminanceModulation2);
            schemeColor32.Append(saturationModulation2);
            schemeColor32.Append(tint2);

            gradientStop2.Append(schemeColor32);

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop3 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 100000 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor33 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation3 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 105000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation3 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 109000 };
            DocumentFormat.OpenXml.Drawing.Tint tint3 = new DocumentFormat.OpenXml.Drawing.Tint() { Val = 81000 };

            schemeColor33.Append(luminanceModulation3);
            schemeColor33.Append(saturationModulation3);
            schemeColor33.Append(tint3);

            gradientStop3.Append(schemeColor33);

            gradientStopList1.Append(gradientStop1);
            gradientStopList1.Append(gradientStop2);
            gradientStopList1.Append(gradientStop3);
            DocumentFormat.OpenXml.Drawing.LinearGradientFill linearGradientFill1 =
                new DocumentFormat.OpenXml.Drawing.LinearGradientFill() { Angle = 5400000, Scaled = false };

            gradientFill1.Append(gradientStopList1);
            gradientFill1.Append(linearGradientFill1);

            DocumentFormat.OpenXml.Drawing.GradientFill gradientFill2 =
                new DocumentFormat.OpenXml.Drawing.GradientFill() { RotateWithShape = true };

            DocumentFormat.OpenXml.Drawing.GradientStopList gradientStopList2 =
                new DocumentFormat.OpenXml.Drawing.GradientStopList();

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop4 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 0 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor34 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation4 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 103000 };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation4 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 102000 };
            DocumentFormat.OpenXml.Drawing.Tint tint4 = new DocumentFormat.OpenXml.Drawing.Tint() { Val = 94000 };

            schemeColor34.Append(saturationModulation4);
            schemeColor34.Append(luminanceModulation4);
            schemeColor34.Append(tint4);

            gradientStop4.Append(schemeColor34);

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop5 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 50000 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor35 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation5 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 110000 };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation5 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 100000 };
            DocumentFormat.OpenXml.Drawing.Shade shade1 = new DocumentFormat.OpenXml.Drawing.Shade() { Val = 100000 };

            schemeColor35.Append(saturationModulation5);
            schemeColor35.Append(luminanceModulation5);
            schemeColor35.Append(shade1);

            gradientStop5.Append(schemeColor35);

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop6 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 100000 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor36 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation6 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 99000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation6 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 120000 };
            DocumentFormat.OpenXml.Drawing.Shade shade2 = new DocumentFormat.OpenXml.Drawing.Shade() { Val = 78000 };

            schemeColor36.Append(luminanceModulation6);
            schemeColor36.Append(saturationModulation6);
            schemeColor36.Append(shade2);

            gradientStop6.Append(schemeColor36);

            gradientStopList2.Append(gradientStop4);
            gradientStopList2.Append(gradientStop5);
            gradientStopList2.Append(gradientStop6);
            DocumentFormat.OpenXml.Drawing.LinearGradientFill linearGradientFill2 =
                new DocumentFormat.OpenXml.Drawing.LinearGradientFill() { Angle = 5400000, Scaled = false };

            gradientFill2.Append(gradientStopList2);
            gradientFill2.Append(linearGradientFill2);

            fillStyleList1.Append(solidFill29);
            fillStyleList1.Append(gradientFill1);
            fillStyleList1.Append(gradientFill2);

            DocumentFormat.OpenXml.Drawing.LineStyleList lineStyleList1 =
                new DocumentFormat.OpenXml.Drawing.LineStyleList();

            DocumentFormat.OpenXml.Drawing.Outline outline1 = new DocumentFormat.OpenXml.Drawing.Outline()
            {
                Width = 6350,
                CapType = DocumentFormat.OpenXml.Drawing.LineCapValues.Flat,
                CompoundLineType = DocumentFormat.OpenXml.Drawing.CompoundLineValues.Single,
                Alignment = DocumentFormat.OpenXml.Drawing.PenAlignmentValues.Center
            };

            DocumentFormat.OpenXml.Drawing.SolidFill solidFill30 = new DocumentFormat.OpenXml.Drawing.SolidFill();
            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor37 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };

            solidFill30.Append(schemeColor37);
            DocumentFormat.OpenXml.Drawing.PresetDash presetDash1 =
                new DocumentFormat.OpenXml.Drawing.PresetDash()
                {
                    Val = DocumentFormat.OpenXml.Drawing.PresetLineDashValues.Solid
                };
            DocumentFormat.OpenXml.Drawing.Miter miter1 = new DocumentFormat.OpenXml.Drawing.Miter() { Limit = 800000 };

            outline1.Append(solidFill30);
            outline1.Append(presetDash1);
            outline1.Append(miter1);

            DocumentFormat.OpenXml.Drawing.Outline outline2 = new DocumentFormat.OpenXml.Drawing.Outline()
            {
                Width = 12700,
                CapType = DocumentFormat.OpenXml.Drawing.LineCapValues.Flat,
                CompoundLineType = DocumentFormat.OpenXml.Drawing.CompoundLineValues.Single,
                Alignment = DocumentFormat.OpenXml.Drawing.PenAlignmentValues.Center
            };

            DocumentFormat.OpenXml.Drawing.SolidFill solidFill31 = new DocumentFormat.OpenXml.Drawing.SolidFill();
            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor38 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };

            solidFill31.Append(schemeColor38);
            DocumentFormat.OpenXml.Drawing.PresetDash presetDash2 =
                new DocumentFormat.OpenXml.Drawing.PresetDash()
                {
                    Val = DocumentFormat.OpenXml.Drawing.PresetLineDashValues.Solid
                };
            DocumentFormat.OpenXml.Drawing.Miter miter2 = new DocumentFormat.OpenXml.Drawing.Miter() { Limit = 800000 };

            outline2.Append(solidFill31);
            outline2.Append(presetDash2);
            outline2.Append(miter2);

            DocumentFormat.OpenXml.Drawing.Outline outline3 = new DocumentFormat.OpenXml.Drawing.Outline()
            {
                Width = 19050,
                CapType = DocumentFormat.OpenXml.Drawing.LineCapValues.Flat,
                CompoundLineType = DocumentFormat.OpenXml.Drawing.CompoundLineValues.Single,
                Alignment = DocumentFormat.OpenXml.Drawing.PenAlignmentValues.Center
            };

            DocumentFormat.OpenXml.Drawing.SolidFill solidFill32 = new DocumentFormat.OpenXml.Drawing.SolidFill();
            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor39 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };

            solidFill32.Append(schemeColor39);
            DocumentFormat.OpenXml.Drawing.PresetDash presetDash3 =
                new DocumentFormat.OpenXml.Drawing.PresetDash()
                {
                    Val = DocumentFormat.OpenXml.Drawing.PresetLineDashValues.Solid
                };
            DocumentFormat.OpenXml.Drawing.Miter miter3 = new DocumentFormat.OpenXml.Drawing.Miter() { Limit = 800000 };

            outline3.Append(solidFill32);
            outline3.Append(presetDash3);
            outline3.Append(miter3);

            lineStyleList1.Append(outline1);
            lineStyleList1.Append(outline2);
            lineStyleList1.Append(outline3);

            DocumentFormat.OpenXml.Drawing.EffectStyleList effectStyleList1 =
                new DocumentFormat.OpenXml.Drawing.EffectStyleList();

            DocumentFormat.OpenXml.Drawing.EffectStyle effectStyle1 = new DocumentFormat.OpenXml.Drawing.EffectStyle();
            DocumentFormat.OpenXml.Drawing.EffectList effectList1 = new DocumentFormat.OpenXml.Drawing.EffectList();

            effectStyle1.Append(effectList1);

            DocumentFormat.OpenXml.Drawing.EffectStyle effectStyle2 = new DocumentFormat.OpenXml.Drawing.EffectStyle();
            DocumentFormat.OpenXml.Drawing.EffectList effectList2 = new DocumentFormat.OpenXml.Drawing.EffectList();

            effectStyle2.Append(effectList2);

            DocumentFormat.OpenXml.Drawing.EffectStyle effectStyle3 = new DocumentFormat.OpenXml.Drawing.EffectStyle();

            DocumentFormat.OpenXml.Drawing.EffectList effectList3 = new DocumentFormat.OpenXml.Drawing.EffectList();

            DocumentFormat.OpenXml.Drawing.OuterShadow outerShadow1 = new DocumentFormat.OpenXml.Drawing.OuterShadow()
            {
                BlurRadius = 57150L,
                Distance = 19050L,
                Direction = 5400000,
                Alignment = DocumentFormat.OpenXml.Drawing.RectangleAlignmentValues.Center,
                RotateWithShape = false
            };

            DocumentFormat.OpenXml.Drawing.RgbColorModelHex rgbColorModelHex11 =
                new DocumentFormat.OpenXml.Drawing.RgbColorModelHex() { Val = "000000" };
            DocumentFormat.OpenXml.Drawing.Alpha alpha1 = new DocumentFormat.OpenXml.Drawing.Alpha() { Val = 63000 };

            rgbColorModelHex11.Append(alpha1);

            outerShadow1.Append(rgbColorModelHex11);

            effectList3.Append(outerShadow1);

            effectStyle3.Append(effectList3);

            effectStyleList1.Append(effectStyle1);
            effectStyleList1.Append(effectStyle2);
            effectStyleList1.Append(effectStyle3);

            DocumentFormat.OpenXml.Drawing.BackgroundFillStyleList backgroundFillStyleList1 =
                new DocumentFormat.OpenXml.Drawing.BackgroundFillStyleList();

            DocumentFormat.OpenXml.Drawing.SolidFill solidFill33 = new DocumentFormat.OpenXml.Drawing.SolidFill();
            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor40 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };

            solidFill33.Append(schemeColor40);

            DocumentFormat.OpenXml.Drawing.SolidFill solidFill34 = new DocumentFormat.OpenXml.Drawing.SolidFill();

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor41 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.Tint tint5 = new DocumentFormat.OpenXml.Drawing.Tint() { Val = 95000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation7 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 170000 };

            schemeColor41.Append(tint5);
            schemeColor41.Append(saturationModulation7);

            solidFill34.Append(schemeColor41);

            DocumentFormat.OpenXml.Drawing.GradientFill gradientFill3 =
                new DocumentFormat.OpenXml.Drawing.GradientFill() { RotateWithShape = true };

            DocumentFormat.OpenXml.Drawing.GradientStopList gradientStopList3 =
                new DocumentFormat.OpenXml.Drawing.GradientStopList();

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop7 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 0 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor42 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.Tint tint6 = new DocumentFormat.OpenXml.Drawing.Tint() { Val = 93000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation8 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 150000 };
            DocumentFormat.OpenXml.Drawing.Shade shade3 = new DocumentFormat.OpenXml.Drawing.Shade() { Val = 98000 };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation7 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 102000 };

            schemeColor42.Append(tint6);
            schemeColor42.Append(saturationModulation8);
            schemeColor42.Append(shade3);
            schemeColor42.Append(luminanceModulation7);

            gradientStop7.Append(schemeColor42);

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop8 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 50000 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor43 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.Tint tint7 = new DocumentFormat.OpenXml.Drawing.Tint() { Val = 98000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation9 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 130000 };
            DocumentFormat.OpenXml.Drawing.Shade shade4 = new DocumentFormat.OpenXml.Drawing.Shade() { Val = 90000 };
            DocumentFormat.OpenXml.Drawing.LuminanceModulation luminanceModulation8 =
                new DocumentFormat.OpenXml.Drawing.LuminanceModulation() { Val = 103000 };

            schemeColor43.Append(tint7);
            schemeColor43.Append(saturationModulation9);
            schemeColor43.Append(shade4);
            schemeColor43.Append(luminanceModulation8);

            gradientStop8.Append(schemeColor43);

            DocumentFormat.OpenXml.Drawing.GradientStop gradientStop9 =
                new DocumentFormat.OpenXml.Drawing.GradientStop() { Position = 100000 };

            DocumentFormat.OpenXml.Drawing.SchemeColor schemeColor44 =
                new DocumentFormat.OpenXml.Drawing.SchemeColor()
                {
                    Val = DocumentFormat.OpenXml.Drawing.SchemeColorValues.PhColor
                };
            DocumentFormat.OpenXml.Drawing.Shade shade5 = new DocumentFormat.OpenXml.Drawing.Shade() { Val = 63000 };
            DocumentFormat.OpenXml.Drawing.SaturationModulation saturationModulation10 =
                new DocumentFormat.OpenXml.Drawing.SaturationModulation() { Val = 120000 };

            schemeColor44.Append(shade5);
            schemeColor44.Append(saturationModulation10);

            gradientStop9.Append(schemeColor44);

            gradientStopList3.Append(gradientStop7);
            gradientStopList3.Append(gradientStop8);
            gradientStopList3.Append(gradientStop9);
            DocumentFormat.OpenXml.Drawing.LinearGradientFill linearGradientFill3 =
                new DocumentFormat.OpenXml.Drawing.LinearGradientFill() { Angle = 5400000, Scaled = false };

            gradientFill3.Append(gradientStopList3);
            gradientFill3.Append(linearGradientFill3);

            backgroundFillStyleList1.Append(solidFill33);
            backgroundFillStyleList1.Append(solidFill34);
            backgroundFillStyleList1.Append(gradientFill3);

            formatScheme1.Append(fillStyleList1, lineStyleList1, effectStyleList1, backgroundFillStyleList1);

            themeElements1.Append(colorScheme1, fontScheme1, formatScheme1);

            DocumentFormat.OpenXml.Drawing.ObjectDefaults objectDefaults1 =
                new DocumentFormat.OpenXml.Drawing.ObjectDefaults();
            DocumentFormat.OpenXml.Drawing.ExtraColorSchemeList extraColorSchemeList1 =
                new DocumentFormat.OpenXml.Drawing.ExtraColorSchemeList();

            DocumentFormat.OpenXml.Drawing.OfficeStyleSheetExtensionList officeStyleSheetExtensionList1 =
                new DocumentFormat.OpenXml.Drawing.OfficeStyleSheetExtensionList();

            DocumentFormat.OpenXml.Drawing.OfficeStyleSheetExtension officeStyleSheetExtension1 =
                new DocumentFormat.OpenXml.Drawing.OfficeStyleSheetExtension()
                {
                    Uri = "{05A4C25C-085E-4340-85A3-A5531E510DB2}"
                };

            DocumentFormat.OpenXml.Office2013.Theme.ThemeFamily themeFamily1 =
                new DocumentFormat.OpenXml.Office2013.Theme.ThemeFamily()
                {
                    Name = "Office Theme",
                    Id = "{62F939B6-93AF-4DB8-9C6B-D6C7DFDC589F}",
                    Vid = "{4A3C46E8-61CC-4603-A589-7422A47A8E4A}"
                };
            themeFamily1.AddNamespaceDeclaration("thm15", "http://schemas.microsoft.com/office/thememl/2012/main");

            officeStyleSheetExtension1.Append(themeFamily1);

            officeStyleSheetExtensionList1.Append(officeStyleSheetExtension1);

            theme1.Append(themeElements1, objectDefaults1, extraColorSchemeList1, officeStyleSheetExtensionList1);

            themePart1.Theme = theme1;
        }
    }
}
