// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml.Packaging;

Console.WriteLine("Hello, World!");

using (var presentationDocument = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(@"C:\lindexi\Office\演示文稿1.pptx", true))
{
    var slidePart = presentationDocument.PresentationPart!.SlideParts.First();

    var slide = slidePart.Slide;
    var chartPart = (DocumentFormat.OpenXml.Packaging.ChartPart) slidePart.GetPartById("rId2");

    var openXmlPart = chartPart.GetPartById("rId3");

    chartPart.DeletePart(openXmlPart);

    var packagePart = chartPart.AddEmbeddedPackagePart(EmbeddedPackagePartType.Xlsx);

}