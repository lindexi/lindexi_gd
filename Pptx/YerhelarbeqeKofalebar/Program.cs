using System;
using System.Linq;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.Drawing;
using Paragraph = DocumentFormat.OpenXml.Drawing.Paragraph;
using Run = DocumentFormat.OpenXml.Drawing.Run;
using RunProperties = DocumentFormat.OpenXml.Drawing.RunProperties;
using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", false);
var presentationPart = presentationDocument.PresentationPart;
var slidePart = presentationPart!.SlideParts.First();
var slide = slidePart.Slide;

var textBody = slide.CommonSlideData!.ShapeTree!.Descendants<TextBody>().First();
Paragraph paragraph = textBody.GetFirstChild<Paragraph>()!;



TextMath textMath = paragraph.GetFirstChild<DocumentFormat.OpenXml.Office2010.Drawing.TextMath>()!;
foreach (var textMathChildElement in textMath.ChildElements)
{
    
}

foreach (var run in paragraph.Elements<Run>())
{
    RunProperties? runProperties = run.RunProperties;
    var baseline = runProperties?.Baseline?.Value;
    var text = run.Text?.Text;
}