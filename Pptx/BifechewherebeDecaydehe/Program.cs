using System;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

using (var presentationDocument =
       DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", true))
{
    SlidePart slidePart = presentationDocument.PresentationPart.SlideParts.FirstOrDefault();

    foreach (var tableCell in slidePart.Slide.CommonSlideData.ShapeTree.Descendants<TableCell>())
    {
        tableCell.RemoveAllChildren();
        var textBody = new TextBody()
        {
            BodyProperties = new BodyProperties(),
            ListStyle = new ListStyle(),
        };
        var paragraph = new Paragraph();
        textBody.Append(paragraph);
        var textRun = new Run();
        paragraph.AddChild(textRun);
        textRun.RunProperties = new RunProperties();
        textRun.RunProperties.AddChild(new SolidFill()
        {
            RgbColorModelHex = new RgbColorModelHex()
            {
                Val = new HexBinaryValue()
                {
                    Value = "FF0000"
                }
            }
        });
        textRun.Text = new Text()
        {
            Text = "123"
        };
        tableCell.AddChild(textBody);
    }
}
Console.WriteLine($"Finish");