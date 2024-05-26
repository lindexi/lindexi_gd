using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Flatten.Framework;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    public static class CommonSlidePartGenerator
    {
        public static Slide AddNewEmptySlide(this PresentationPart presentationPart)
        {
            var slidePart = presentationPart.AddNewSlidePart();

            var defaultSlideMasterPart = presentationPart.SlideMasterParts.First();
            var slideLayoutPart = defaultSlideMasterPart.GenerateSlideLayoutPart();

            slidePart.AddPartWithGenerateId(slideLayoutPart);

            return slidePart.Slide;
        }

        private static SlidePart AddNewSlidePart(this PresentationPart presentationPart)
        {
            var (slidePart, id) = presentationPart.AddNewPartWithGenerateId<SlidePart>();

            var slideIdList = presentationPart.Presentation.GetOrCreateElement<SlideIdList>();
            // 页面 Id 要求大于 255 = byte.MaxValue
            SlideId slideId = new SlideId() { Id = (uint) slideIdList.Count() + byte.MaxValue + 1, RelationshipId = id };
            slideIdList.Append(new OpenXmlElement[] { slideId });

            GenerateSlidePartContent(slidePart);

            return slidePart;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="slidePart1"></param>
        // Generates content of slidePart1.
        private static void GenerateSlidePartContent(SlidePart slidePart1)
        {
            Slide slide1 = new Slide();
            slide1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            slide1.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            slide1.AddNamespaceDeclaration("p", "http://schemas.openxmlformats.org/presentationml/2006/main");

            CommonSlideData commonSlideData1 = new CommonSlideData();

            ShapeTree shapeTree1 = new ShapeTree();

            NonVisualGroupShapeProperties nonVisualGroupShapeProperties1 = new NonVisualGroupShapeProperties();
            NonVisualDrawingProperties nonVisualDrawingProperties1 =
                new NonVisualDrawingProperties() { Id = (UInt32Value) 1U, Name = "" };
            NonVisualGroupShapeDrawingProperties nonVisualGroupShapeDrawingProperties1 =
                new NonVisualGroupShapeDrawingProperties();
            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties1 =
                new ApplicationNonVisualDrawingProperties();

            nonVisualGroupShapeProperties1.Append(nonVisualDrawingProperties1);
            nonVisualGroupShapeProperties1.Append(nonVisualGroupShapeDrawingProperties1);
            nonVisualGroupShapeProperties1.Append(applicationNonVisualDrawingProperties1);

            GroupShapeProperties groupShapeProperties1 = new GroupShapeProperties();

            DocumentFormat.OpenXml.Drawing.TransformGroup transformGroup1 = new DocumentFormat.OpenXml.Drawing.TransformGroup();
            DocumentFormat.OpenXml.Drawing.Offset offset1 = new DocumentFormat.OpenXml.Drawing.Offset() { X = 0L, Y = 0L };
            DocumentFormat.OpenXml.Drawing.Extents extents1 = new DocumentFormat.OpenXml.Drawing.Extents() { Cx = 0L, Cy = 0L };
            DocumentFormat.OpenXml.Drawing.ChildOffset childOffset1 = new DocumentFormat.OpenXml.Drawing.ChildOffset() { X = 0L, Y = 0L };
            DocumentFormat.OpenXml.Drawing.ChildExtents childExtents1 = new DocumentFormat.OpenXml.Drawing.ChildExtents() { Cx = 0L, Cy = 0L };

            transformGroup1.Append(offset1);
            transformGroup1.Append(extents1);
            transformGroup1.Append(childOffset1);
            transformGroup1.Append(childExtents1);

            groupShapeProperties1.Append(transformGroup1);

            shapeTree1.Append(nonVisualGroupShapeProperties1);
            shapeTree1.Append(groupShapeProperties1);
            //shapeTree1.Append(picture1);

            CommonSlideDataExtensionList commonSlideDataExtensionList1 = new CommonSlideDataExtensionList();

            CommonSlideDataExtension commonSlideDataExtension1 =
                new CommonSlideDataExtension() { Uri = "{BB962C8B-B14F-4D97-AF65-F5344CB8AC3E}" };

            DocumentFormat.OpenXml.Office2010.PowerPoint.CreationId creationId1 = new DocumentFormat.OpenXml.Office2010.PowerPoint.CreationId() { Val = (UInt32Value) 840519474U };
            creationId1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

            commonSlideDataExtension1.Append(creationId1);

            commonSlideDataExtensionList1.Append(commonSlideDataExtension1);

            commonSlideData1.Append(shapeTree1);
            commonSlideData1.Append(commonSlideDataExtensionList1);

            ColorMapOverride colorMapOverride1 = new ColorMapOverride();
            DocumentFormat.OpenXml.Drawing.MasterColorMapping masterColorMapping1 = new DocumentFormat.OpenXml.Drawing.MasterColorMapping();

            colorMapOverride1.Append(masterColorMapping1);

            slide1.Append(commonSlideData1);
            slide1.Append(colorMapOverride1);

            slidePart1.Slide = slide1;
        }
    }
}
