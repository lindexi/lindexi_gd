using System;
using System.Diagnostics;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace RurlejileGearhuheljale
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var presentationDocument = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open("自定义形状.pptx", false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                var presentation = presentationPart.Presentation;

                var slideIdList = presentation.SlideIdList;

                foreach (var slideId in slideIdList.ChildElements.OfType<SlideId>())
                {
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId);

                    var slide = slidePart.Slide;

                    var customGeometry = slide.Descendants<CustomGeometry>().First();
                    var sharpTextRectangle = customGeometry.Rectangle;
                }
            }
        }
    }
}
