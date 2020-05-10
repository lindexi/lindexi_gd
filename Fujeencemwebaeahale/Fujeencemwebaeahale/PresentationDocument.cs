using DocumentFormat.OpenXml.Packaging;

namespace DocumentFormat.OpenXml.Packaging
{
    public partial class PresentationDocument
    {
        // The New API
        public static PresentationDocument Open(DocumentFormat.OpenXml.Packaging.IPackage package)
        {
            return PresentationDocument.Open(package);
        }

        public static PresentationDocument Open(System.IO.Packaging.Package package)
        {
            IPackage packageAdapt = new PackageAdapt(package);
            return PresentationDocument.Open(packageAdapt);
        }
    }
}