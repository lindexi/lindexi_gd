using DocumentFormat.OpenXml.Packaging;

namespace JufokawnelWhelkefeeqayje.Framework
{
    public static class OpenXmlPartExtension
    {
        public static (T part, string id) AddNewPartWithGenerateId<T>(this OpenXmlPartContainer container)
            where T : OpenXmlPart, IFixedContentTypePart
        {
            var id = container.GenerateNewId();
            return (container.AddNewPart<T>(id), id);
        }

        public static (T part, string id) AddNewPartWithGenerateId<T>(this OpenXmlPartContainer container, string contentType)
            where T : OpenXmlPart, IFixedContentTypePart
        {
            var id = container.GenerateNewId();
            return (container.AddNewPart<T>(contentType, id), id);
        }

        public static (T part, string id) AddPartWithGenerateId<T>(this OpenXmlPartContainer container, T part)
            where T : OpenXmlPart, IFixedContentTypePart
        {
            var id = container.GenerateNewId();
            return (container.AddPart(part, id), id);
        }

        public static string GenerateNewId(this OpenXmlPartContainer container) => "rId" + (container.Parts.Count() + 1);
    }
}
