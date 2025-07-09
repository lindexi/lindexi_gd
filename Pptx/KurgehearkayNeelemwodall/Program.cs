using System;
using System.Linq;

using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", true);
var presentationPart = presentationDocument.PresentationPart;
var slideIndex = 0;
foreach (SlidePart slidePart in presentationPart.SlideParts)
{
    Console.WriteLine($"SlideIndex={slideIndex}");
    slideIndex++;
}