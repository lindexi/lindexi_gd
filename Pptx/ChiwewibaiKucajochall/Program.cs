using System;
using System.Linq;

using DocumentFormat.OpenXml.Drawing;

using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", true);
var presentationPart = presentationDocument.PresentationPart;
var slidePart = presentationPart!.SlideParts.First();
var slide = slidePart.Slide;

var textBody = slide.CommonSlideData!.ShapeTree!.Descendants<TextBody>().First();
Paragraph paragraph = textBody.GetFirstChild<Paragraph>()!;

foreach (TextAutoNumberSchemeValues value in Enum.GetValues<TextAutoNumberSchemeValues>())
{
    for (int i = 0; i < 3; i++)
    {
        var currentParagraph = (Paragraph) paragraph.CloneNode(deep: true);
        AutoNumberedBullet autoNumberedBullet = currentParagraph.ParagraphProperties!.GetFirstChild<AutoNumberedBullet>()!;
        autoNumberedBullet.Type = value;
        Run run = currentParagraph.GetFirstChild<Run>()!;
        run.Text = new Text($"{value} {i + 1}");

        textBody.AppendChild(currentParagraph);
    }

}
textBody.RemoveChild(paragraph);
