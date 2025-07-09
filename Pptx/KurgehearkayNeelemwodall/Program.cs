using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Path = System.IO.Path;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

var pptxFile = Path.Join(AppContext.BaseDirectory, "Test.pptx");
if (args.Length == 1)
{
    pptxFile = args[0];
}

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(pptxFile, true);
var presentationPart = presentationDocument.PresentationPart;
var slideIndex = 0;
var slideIdList = presentationPart.Presentation.SlideIdList;
foreach (var slideId in slideIdList.OfType<SlideId>())
{
    Console.WriteLine($"SlideIndex={slideIndex} SlideId={slideId.Id}");
    slideIndex++;

    var slidePart = (SlidePart) presentationPart.GetPartById(slideId.RelationshipId);
    var slide = slidePart.Slide;
    foreach (var openXmlElement in slide.CommonSlideData.ShapeTree)
    {
        if (openXmlElement is DocumentFormat.OpenXml.AlternateContent alternateContent)
        {
            // +		[0]	{DocumentFormat.OpenXml.AlternateContentChoice}	DocumentFormat.OpenXml.OpenXmlElement {DocumentFormat.OpenXml.AlternateContentChoice}
            // 
            var alternateContentChoice = alternateContent.GetFirstChild<DocumentFormat.OpenXml.AlternateContentChoice>();
            var textBody = alternateContentChoice.Descendants<TextBody>().First();

            Console.WriteLine($"段落数量： {textBody.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>().Count()}");
        }
    }
}
