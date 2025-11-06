using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using System;
using System.Linq;
using GroupShape = DocumentFormat.OpenXml.Presentation.GroupShape;
using NonVisualGroupShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualGroupShapeProperties;

var pptxFile = @"F:\temp\LalwherejawyerRejulofelbenel\test.pptx";
if (args.Length == 1)
{
    pptxFile = args[0];
}

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(pptxFile, true);
var presentationPart = presentationDocument.PresentationPart!;
var slideIndex = 0;
var slideIdList = presentationPart.Presentation.SlideIdList!;
foreach (var slideId in slideIdList.OfType<SlideId>())
{
    Console.WriteLine($"SlideIndex={slideIndex} SlideId={slideId.Id}");
    slideIndex++;

    var slidePart = (SlidePart) presentationPart.GetPartById(slideId.RelationshipId!);
    var slide = slidePart.Slide;

    foreach (var openXmlElement in slide.CommonSlideData!.ShapeTree!.Elements())
    {
        if (openXmlElement is GroupShape groupShape)
        {
            NonVisualGroupShapeProperties nonVisualGroupShapeProperties = groupShape.NonVisualGroupShapeProperties!;
            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties = nonVisualGroupShapeProperties.ApplicationNonVisualDrawingProperties!;
            var customerDataList = applicationNonVisualDrawingProperties.GetFirstChild<CustomerDataList>();
        }
    }
}