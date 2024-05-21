using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using dotnetCampus.OpenXmlUnitConverter;
using JufokawnelWhelkefeeqayje.Framework.Context;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    public static class CommonPresentationDocumentGenerator
    {
        /// <summary>
        /// 创建一份空白的文档
        /// </summary>
        /// <returns></returns>
        public static PresentationDocument CreateEmptyDocument(FileInfo file, DocumentInfo documentInfo)
        {
            var filePath = file.FullName;
            PresentationDocument package =
                PresentationDocument.Create(filePath, PresentationDocumentType.Presentation);

            CommonPresentationDocumentGenerator.FillDefaultEmptyDocument(package, documentInfo);
            return package;
        }

        /// <summary>
        /// 填充好默认的文档内容
        /// </summary>
        /// <param name="document"></param>
        /// <param name="documentInfo"></param>
        /// <returns></returns>
        public static PresentationDocument FillDefaultEmptyDocument(PresentationDocument document,
            DocumentInfo documentInfo)
        {
            CreateParts(document, documentInfo);
            return document;
        }

        // Adds child parts and generates content of the specified part.
        private static void CreateParts(PresentationDocument document, DocumentInfo documentInfo)
        {
            PresentationPart presentationPart1 =
                document.GeneratePresentationPart();
            // 为了不污染 CommonPresentationPartGenerator 的逻辑，因此重新写设置页面大小逻辑
            SetPresentationPart(presentationPart1, documentInfo);

            SlideMasterPart slideMasterPart1 = presentationPart1.AddNewSlideMasterPart();

            presentationPart1.AddNewEmptySlide();

            var defaultThemePart = presentationPart1.GetPartsOfType<ThemePart>().First();
            slideMasterPart1.AddPartWithGenerateId(defaultThemePart);

            document.GenerateExtendedFilePropertiesPart();

            SetPackageProperties(document, documentInfo);

            SetCustomFileProperties(document, documentInfo);
        }

        private static void SetPresentationPart(PresentationPart presentationPart, DocumentInfo documentInfo)
        {
            presentationPart.Presentation.SlideSize = new SlideSize()
            {
                Cx = documentInfo.SlideSize.Width.ToOpenXmlInt32Value(),
                Cy = documentInfo.SlideSize.Height.ToOpenXmlInt32Value()
            };
        }

        private static void SetPackageProperties(OpenXmlPackage document, DocumentInfo documentInfo)
        {
            document.PackageProperties.Creator = documentInfo.Creator;
            document.PackageProperties.Title = documentInfo.Title;
            document.PackageProperties.Revision = "1";
            document.PackageProperties.Created = DateTime.Now;
            document.PackageProperties.Modified = DateTime.Now;
            document.PackageProperties.LastModifiedBy = documentInfo.LastModifiedBy;
        }

        private static void SetCustomFileProperties(PresentationDocument document, DocumentInfo documentInfo)
        {
            var applicationName = documentInfo.ApplicationName;
            if (!string.IsNullOrEmpty(applicationName))
            {
                document.SetCustomDocumentProperty("ApplicationName", applicationName!);
            }

            var applicationVersion = documentInfo.ApplicationVersion;
            if (!string.IsNullOrEmpty(applicationVersion))
            {
                document.SetCustomDocumentProperty("ApplicationVersion", applicationVersion!);
            }
        }
    }
}
