using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Ap = DocumentFormat.OpenXml.ExtendedProperties;
using Vt = DocumentFormat.OpenXml.VariantTypes;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonExtendedFilePropertiesPartGenerator
    {
        public static ExtendedFilePropertiesPart GenerateExtendedFilePropertiesPart(this PresentationDocument document)
        {
            var (extendedFilePropertiesPart1, _) =
                document.AddNewPartWithGenerateId<ExtendedFilePropertiesPart>();
            GenerateExtendedFilePropertiesPart1Content(extendedFilePropertiesPart1);
            return extendedFilePropertiesPart1;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="extendedFilePropertiesPart1"></param>
        // Generates content of extendedFilePropertiesPart1.
        private static void GenerateExtendedFilePropertiesPart1Content(
            ExtendedFilePropertiesPart extendedFilePropertiesPart1)
        {
            Ap.TotalTime totalTime = new Ap.TotalTime {Text = "0"};
            Ap.Words words = new Ap.Words {Text = "0"};
            Ap.Application application = new Ap.Application
            {
                // 不能设置 ExtendedFilePropertiesPart 的内容，否则 PPT 将打不开
                // 下面固定是 Microsoft Office PowerPoint 字符串
                Text = "Microsoft Office PowerPoint"
            };

            Ap.PresentationFormat presentationFormat = new Ap.PresentationFormat {Text = "宽屏"};
            Ap.Paragraphs paragraphs = new Ap.Paragraphs {Text = "0"};
            Ap.Slides slides = new Ap.Slides {Text = "1"};
            Ap.Notes notes = new Ap.Notes {Text = "0"};
            Ap.HiddenSlides hiddenSlides = new Ap.HiddenSlides {Text = "0"};
            Ap.MultimediaClips multimediaClips = new Ap.MultimediaClips {Text = "0"};
            Ap.ScaleCrop scaleCrop = new Ap.ScaleCrop {Text = "false"};

            Ap.HeadingPairs headingPairs = new Ap.HeadingPairs();

            headingPairs.AppendChild(new Vt.VTVector
            (
                new Vt.Variant(new Vt.VTLPSTR {Text = "已用的字体"}),
                new Vt.Variant(new Vt.VTInt32 {Text = "1"}),
                new Vt.Variant(new Vt.VTLPSTR {Text = "主题"}),
                new Vt.Variant(new Vt.VTInt32 {Text = "1"}),
                new Vt.Variant(new Vt.VTLPSTR {Text = "幻灯片标题"}),
                new Vt.Variant(new Vt.VTInt32 {Text = "1"})
            ) {BaseType = Vt.VectorBaseValues.Variant, Size = (UInt32Value) 6U});

            Ap.TitlesOfParts titlesOfParts = new Ap.TitlesOfParts(new Vt.VTVector
            (
                new Vt.VTLPSTR {Text = "Arial"},
                new Vt.VTLPSTR {Text = "Office 主题​​"},
                new Vt.VTLPSTR {Text = "PowerPoint 演示文稿"})
            {
                BaseType = Vt.VectorBaseValues.Lpstr, Size = (UInt32Value) 3U
            });

            Ap.Company company = new Ap.Company {Text = ""};
            Ap.LinksUpToDate linksUpToDate = new Ap.LinksUpToDate {Text = "false"};
            Ap.SharedDocument sharedDocument = new Ap.SharedDocument {Text = "false"};
            Ap.HyperlinksChanged hyperlinksChanged = new Ap.HyperlinksChanged {Text = "false"};
            Ap.ApplicationVersion applicationVersion = new Ap.ApplicationVersion {Text = "16.0000"};

            Ap.Properties properties = new Ap.Properties
            (
                totalTime,
                words,
                application,
                presentationFormat,
                paragraphs,
                slides,
                notes,
                hiddenSlides,
                multimediaClips,
                scaleCrop,
                headingPairs,
                titlesOfParts,
                company,
                linksUpToDate,
                sharedDocument,
                hyperlinksChanged,
                applicationVersion
            );
            properties.AddNamespaceDeclaration("vt",
                "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes");

            extendedFilePropertiesPart1.Properties = properties;
        }
    }
}
