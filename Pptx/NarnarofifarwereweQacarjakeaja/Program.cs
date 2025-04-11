using System;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using GraphicFrame = DocumentFormat.OpenXml.Presentation.GraphicFrame;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualShapeProperties;

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", false);
var presentationPart = presentationDocument.PresentationPart;
var slidePart = presentationPart!.SlideParts.First();
var slide = slidePart.Slide;

var graphicFrame = slide.CommonSlideData!.ShapeTree!.GetFirstChild<GraphicFrame>()!;
var graphic = graphicFrame.Graphic!;
var graphicData = graphic.GraphicData!;
var table = graphicData.GetFirstChild<Table>()!; // a:tbl
foreach (var tableCell in table.Descendants<TableCell>())
{
    if (tableCell.HorizontalMerge?.Value == true)
    {
        Console.WriteLine($"单元格被合并");
    }
}