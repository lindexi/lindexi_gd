var file = "A shared part is referenced by multiple source parts with a different relationship type.pptx";

using var presentationDocument = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(file, false);

// DocumentFormat.OpenXml.Packaging.OpenXmlPackageException: 'A shared part is referenced by multiple source parts with a different relationship type.'
