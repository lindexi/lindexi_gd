using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonViewPropertiesPartGenerator
    {
        public static ViewPropertiesPart GenerateViewPropertiesPart(this PresentationPart presentationPart)
        {
            var (viewPropertiesPart, _) = presentationPart.AddNewPartWithGenerateId<ViewPropertiesPart>();
            GenerateViewPropertiesPart1Content(viewPropertiesPart);
            return viewPropertiesPart;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="viewPropertiesPart1"></param>
        // Generates content of viewPropertiesPart1.
        private static void GenerateViewPropertiesPart1Content(ViewPropertiesPart viewPropertiesPart1)
        {
            ViewProperties viewProperties1 = new ViewProperties();
            viewProperties1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            viewProperties1.AddNamespaceDeclaration("r",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            viewProperties1.AddNamespaceDeclaration("p", "http://schemas.openxmlformats.org/presentationml/2006/main");

            NormalViewProperties normalViewProperties1 =
                new NormalViewProperties() {HorizontalBarState = SplitterBarStateValues.Maximized};
            RestoredLeft restoredLeft1 = new RestoredLeft() {Size = 15987, AutoAdjust = false};
            RestoredTop restoredTop1 = new RestoredTop() {Size = 94660};

            normalViewProperties1.Append(restoredLeft1);
            normalViewProperties1.Append(restoredTop1);

            SlideViewProperties slideViewProperties1 = new SlideViewProperties();

            CommonSlideViewProperties commonSlideViewProperties1 = new CommonSlideViewProperties() {SnapToGrid = false};

            CommonViewProperties commonViewProperties1 = new CommonViewProperties() {VariableScale = true};

            ScaleFactor scaleFactor1 = new ScaleFactor();
            A.ScaleX scaleX1 = new A.ScaleX() {Numerator = 107, Denominator = 100};
            A.ScaleY scaleY1 = new A.ScaleY() {Numerator = 107, Denominator = 100};

            scaleFactor1.Append(scaleX1);
            scaleFactor1.Append(scaleY1);
            Origin origin1 = new Origin() {X = 606L, Y = 114L};

            commonViewProperties1.Append(scaleFactor1);
            commonViewProperties1.Append(origin1);
            GuideList guideList1 = new GuideList();

            commonSlideViewProperties1.Append(commonViewProperties1);
            commonSlideViewProperties1.Append(guideList1);

            slideViewProperties1.Append(commonSlideViewProperties1);

            NotesTextViewProperties notesTextViewProperties1 = new NotesTextViewProperties();

            CommonViewProperties commonViewProperties2 = new CommonViewProperties();

            ScaleFactor scaleFactor2 = new ScaleFactor();
            A.ScaleX scaleX2 = new A.ScaleX() {Numerator = 1, Denominator = 1};
            A.ScaleY scaleY2 = new A.ScaleY() {Numerator = 1, Denominator = 1};

            scaleFactor2.Append(scaleX2);
            scaleFactor2.Append(scaleY2);
            Origin origin2 = new Origin() {X = 0L, Y = 0L};

            commonViewProperties2.Append(scaleFactor2);
            commonViewProperties2.Append(origin2);

            notesTextViewProperties1.Append(commonViewProperties2);
            GridSpacing gridSpacing1 = new GridSpacing() {Cx = 72008L, Cy = 72008L};

            viewProperties1.Append(normalViewProperties1);
            viewProperties1.Append(slideViewProperties1);
            viewProperties1.Append(notesTextViewProperties1);
            viewProperties1.Append(gridSpacing1);

            viewPropertiesPart1.ViewProperties = viewProperties1;
        }
    }
}
