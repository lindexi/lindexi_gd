using System;
using System.Diagnostics;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace RurlejileGearhuheljale
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var presentationDocument = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("1.pptx", false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                var presentation = presentationPart.Presentation;

                var slideIdList = presentation.SlideIdList;

                foreach (var slideId in slideIdList.ChildElements.OfType<SlideId>())
                {
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId);

                    var slide = slidePart.Slide;

                    var background = slide.CommonSlideData.Background;
                    var backgroundProperties = background.BackgroundProperties;
                    var solidFill = backgroundProperties.GetFirstChild<SolidFill>();
                    var solidFillRgbColorModelHex = solidFill.RgbColorModelHex;
                    var alpha = solidFillRgbColorModelHex.GetFirstChild<Alpha>();
                    try
                    {
                        int alphaVal = alpha.Val;
                    }
                    catch (Exception e)
                    {
                        // Input string was not in a correct format.
                        Console.WriteLine(e);
                    }
                }
            }
        }
    }
}
