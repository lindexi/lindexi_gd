using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using P15 = DocumentFormat.OpenXml.Office2013.PowerPoint;
using P14 = DocumentFormat.OpenXml.Office2010.PowerPoint;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    static class CommonPresentationPropertiesPartGenerator
    {
        public static PresentationPropertiesPart GeneratePresentationPropertiesPart(this PresentationPart presentationPart)
        {
            var (presentationPropertiesPart, _) =
                presentationPart.AddNewPartWithGenerateId<PresentationPropertiesPart>();

            GeneratePresentationPropertiesPart1Content(presentationPropertiesPart);

            return presentationPropertiesPart;
        }

        /// <summary>
        /// 此代码是生成代码，通过 OpenXmlSdkTool.exe 生成
        /// </summary>
        /// <param name="presentationPropertiesPart1"></param>
        // Generates content of presentationPropertiesPart1.
        private static void GeneratePresentationPropertiesPart1Content(PresentationPropertiesPart presentationPropertiesPart1)
        {
            PresentationProperties presentationProperties1 = new PresentationProperties();
            presentationProperties1.AddNamespaceDeclaration("a",
                "http://schemas.openxmlformats.org/drawingml/2006/main");
            presentationProperties1.AddNamespaceDeclaration("r",
                "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
            presentationProperties1.AddNamespaceDeclaration("p",
                "http://schemas.openxmlformats.org/presentationml/2006/main");

            PresentationPropertiesExtensionList presentationPropertiesExtensionList1 =
                new PresentationPropertiesExtensionList();

            PresentationPropertiesExtension presentationPropertiesExtension1 =
                new PresentationPropertiesExtension() { Uri = "{E76CE94A-603C-4142-B9EB-6D1370010A27}" };

            P14.DiscardImageEditData discardImageEditData1 = new P14.DiscardImageEditData() { Val = false };
            discardImageEditData1.AddNamespaceDeclaration("p14",
                "http://schemas.microsoft.com/office/powerpoint/2010/main");

            presentationPropertiesExtension1.Append(discardImageEditData1);

            PresentationPropertiesExtension presentationPropertiesExtension2 =
                new PresentationPropertiesExtension() { Uri = "{D31A062A-798A-4329-ABDD-BBA856620510}" };

            P14.DefaultImageDpi defaultImageDpi1 = new P14.DefaultImageDpi() { Val = (UInt32Value) 220U };
            defaultImageDpi1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

            presentationPropertiesExtension2.Append(defaultImageDpi1);

            PresentationPropertiesExtension presentationPropertiesExtension3 =
                new PresentationPropertiesExtension() { Uri = "{FD5EFAAD-0ECE-453E-9831-46B23BE46B34}" };

            P15.ChartTrackingReferenceBased chartTrackingReferenceBased1 =
                new P15.ChartTrackingReferenceBased() { Val = true };
            chartTrackingReferenceBased1.AddNamespaceDeclaration("p15",
                "http://schemas.microsoft.com/office/powerpoint/2012/main");

            presentationPropertiesExtension3.Append(chartTrackingReferenceBased1);

            presentationPropertiesExtensionList1.Append(presentationPropertiesExtension1);
            presentationPropertiesExtensionList1.Append(presentationPropertiesExtension2);
            presentationPropertiesExtensionList1.Append(presentationPropertiesExtension3);

            presentationProperties1.Append(presentationPropertiesExtensionList1);

            presentationPropertiesPart1.PresentationProperties = presentationProperties1;
        }
    }
}
