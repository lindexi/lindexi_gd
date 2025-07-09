using System;
using System.Linq;

using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

using Path = System.IO.Path;

var pptxFile = Path.Join(AppContext.BaseDirectory, "Test.pptx");
if (args.Length == 1)
{
    pptxFile = args[0];
}

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(pptxFile, true);
var presentationPart = presentationDocument.PresentationPart;
var slideIndex = 0;
foreach (SlidePart slidePart in presentationPart.SlideParts)
{
    Console.WriteLine($"SlideIndex={slideIndex}");
    slideIndex++;
}