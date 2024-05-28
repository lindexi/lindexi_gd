using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;

namespace JufokawnelWhelkefeeqayje.Framework.CommonGenerator
{
    public static class CustomFilePropertiesExtensions
    {
        public static PresentationDocument SetCustomDocumentProperty(this PresentationDocument document, string name,
            string value)
        {
            return SetCustomDocumentProperty(document, name, new DocumentFormat.OpenXml.VariantTypes.VTLPWSTR {Text = value});
        }

        public static PresentationDocument SetCustomDocumentProperty(this PresentationDocument document, string name, OpenXmlElement element)
        {
            var customFilePropertiesPart = document.GetPartsOfType<CustomFilePropertiesPart>().FirstOrDefault();
            if (customFilePropertiesPart is null)
            {
                customFilePropertiesPart = document.AddCustomFilePropertiesPart();

                DocumentFormat.OpenXml.CustomProperties.Properties properties = new DocumentFormat.OpenXml.CustomProperties.Properties();
                properties.AddNamespaceDeclaration("vt", "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes");
                customFilePropertiesPart.Properties = properties;
            }

            var customFileProperties = customFilePropertiesPart.Properties;

            var count = customFileProperties.Count();
            DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty customDocumentProperty = new DocumentFormat.OpenXml.CustomProperties.CustomDocumentProperty()
            {
                // 以下的 Guid 是固定的
                FormatId = "{D5CDD505-2E9C-101B-9397-08002B2CF9AE}",
                // 起始值就是从 2 开始，参阅 15.2.12.2 Custom File Properties Part
                // 可以绕过 2 开始，也就是从大于 2 开始是可以的
                PropertyId = 2 + count,
                Name = name
            };
            customDocumentProperty.AppendChild(element);

            customFileProperties.AppendChild(customDocumentProperty);

            return document;
        }
    }
}
