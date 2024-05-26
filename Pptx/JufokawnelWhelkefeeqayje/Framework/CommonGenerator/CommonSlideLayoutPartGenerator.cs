using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Flatten.Framework;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P14 = DocumentFormat.OpenXml.Office2010.PowerPoint;
using ApplicationNonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.ApplicationNonVisualDrawingProperties;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualGroupShapeDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualGroupShapeDrawingProperties;
using NonVisualGroupShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualGroupShapeProperties;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonSlideLayoutPartGenerator
    {
        public static SlideLayoutPart GenerateSlideLayoutPart(this SlideMasterPart slideMasterPart)
        {
            // 多个页面可以有相同的 SlideLayoutPart 元素
            var (slideLayoutPart, id) = slideMasterPart.AddNewPartWithGenerateId<SlideLayoutPart>();
            slideLayoutPart.AddPartWithGenerateId(slideMasterPart);

            GenerateSlideLayoutPartContent(slideLayoutPart);

            var slideLayoutIdList = slideMasterPart.SlideMaster.GetOrCreateElement< SlideLayoutIdList>();

            // 页面版式要求 Id 大于 2147483647 + 1 = int.MaxValue + 1
            SlideLayoutId slideLayoutId1 =
                new SlideLayoutId() {Id = int.MaxValue + (uint)slideLayoutIdList.Count() + 2, RelationshipId = id };

            slideLayoutIdList.Append(slideLayoutId1);

            return slideLayoutPart;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="slideLayoutPart1"></param>
        // Generates content of slideLayoutPart1.
        private static void GenerateSlideLayoutPartContent(SlideLayoutPart slideLayoutPart1)
        {
            SlideLayout slideLayout1 = new SlideLayout() {Preserve = true, UserDrawn = true};
            slideLayout1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            slideLayout1.AddNamespaceDeclaration("r",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            slideLayout1.AddNamespaceDeclaration("p", "http://schemas.openxmlformats.org/presentationml/2006/main");

            CommonSlideData commonSlideData2 = new CommonSlideData() {Name = "标题幻灯片"};

            ShapeTree shapeTree2 = new ShapeTree();

            NonVisualGroupShapeProperties nonVisualGroupShapeProperties2 = new NonVisualGroupShapeProperties();
            NonVisualDrawingProperties nonVisualDrawingProperties3 =
                new NonVisualDrawingProperties() {Id = (UInt32Value) 1U, Name = ""};
            NonVisualGroupShapeDrawingProperties nonVisualGroupShapeDrawingProperties2 =
                new NonVisualGroupShapeDrawingProperties();
            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties3 =
                new ApplicationNonVisualDrawingProperties();

            nonVisualGroupShapeProperties2.Append(nonVisualDrawingProperties3);
            nonVisualGroupShapeProperties2.Append(nonVisualGroupShapeDrawingProperties2);
            nonVisualGroupShapeProperties2.Append(applicationNonVisualDrawingProperties3);

            GroupShapeProperties groupShapeProperties2 = new GroupShapeProperties();

            A.TransformGroup transformGroup2 = new A.TransformGroup();
            A.Offset offset3 = new A.Offset() {X = 0L, Y = 0L};
            A.Extents extents3 = new A.Extents() {Cx = 0L, Cy = 0L};
            A.ChildOffset childOffset2 = new A.ChildOffset() {X = 0L, Y = 0L};
            A.ChildExtents childExtents2 = new A.ChildExtents() {Cx = 0L, Cy = 0L};

            transformGroup2.Append(offset3);
            transformGroup2.Append(extents3);
            transformGroup2.Append(childOffset2);
            transformGroup2.Append(childExtents2);

            groupShapeProperties2.Append(transformGroup2);

            shapeTree2.Append(nonVisualGroupShapeProperties2);
            shapeTree2.Append(groupShapeProperties2);

            CommonSlideDataExtensionList commonSlideDataExtensionList2 = new CommonSlideDataExtensionList();

            CommonSlideDataExtension commonSlideDataExtension2 =
                new CommonSlideDataExtension() {Uri = "{BB962C8B-B14F-4D97-AF65-F5344CB8AC3E}"};

            P14.CreationId creationId2 = new P14.CreationId() {Val = (UInt32Value) 2723191420U};
            creationId2.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

            commonSlideDataExtension2.Append(creationId2);

            commonSlideDataExtensionList2.Append(commonSlideDataExtension2);

            commonSlideData2.Append(shapeTree2);
            commonSlideData2.Append(commonSlideDataExtensionList2);

            ColorMapOverride colorMapOverride2 = new ColorMapOverride();
            A.MasterColorMapping masterColorMapping2 = new A.MasterColorMapping();

            colorMapOverride2.Append(masterColorMapping2);

            slideLayout1.Append(commonSlideData2);
            slideLayout1.Append(colorMapOverride2);

            slideLayoutPart1.SlideLayout = slideLayout1;
        }
    }
}
