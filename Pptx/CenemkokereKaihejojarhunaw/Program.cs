using System;
using System.Linq;

using DocumentFormat.OpenXml.Drawing;

using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", false);
var presentationPart = presentationDocument.PresentationPart;
var slidePart = presentationPart!.SlideParts.First();
var slide = slidePart.Slide;

var textBody = slide.CommonSlideData!.ShapeTree!.Descendants<TextBody>().First();
Paragraph paragraph = textBody.GetFirstChild<Paragraph>()!;

foreach (var run in paragraph.Elements<Run>())
{
    RunProperties? runProperties = run.RunProperties;
    var baseline = runProperties?.Baseline?.Value;
    var text = run.Text?.Text;
}
