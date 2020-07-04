using System;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace ChihilaygerYadekearhu
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var presentationDocument =
                PresentationDocument.Open("test.pptx", false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                foreach (var slidePart in presentationPart.SlideParts)
                {
                    var slide = slidePart.Slide;

                    var slideData = slide.CommonSlideData;
                    var shapeTree = slideData.ShapeTree;

                    foreach (var openXmlElement in shapeTree)
                    {
                        if (openXmlElement is DocumentFormat.OpenXml.Presentation.Shape shape)
                        {

                        }
                    }
                }
            }
        }
    }
}
