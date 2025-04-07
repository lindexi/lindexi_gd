using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

using (var presentationDocument =
       DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("Test.pptx", true))
{
    SlidePart slidePart = presentationDocument.PresentationPart.SlideParts.FirstOrDefault();
    Run textRun = slidePart.Slide.CommonSlideData.ShapeTree.Descendants<Run>().FirstOrDefault(t=>t.Text.Text == "123");
    var runProperties = textRun.RunProperties;
    runProperties.AddChild(new SolidFill()
    {
        RgbColorModelHex = new RgbColorModelHex()
        {
            Val = new HexBinaryValue()
            {
                Value = "FF0000"
            }
        }
    });
}
Console.WriteLine($"Finish");