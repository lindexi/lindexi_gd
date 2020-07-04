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
                    var slideLayoutPart = slidePart.SlideLayoutPart;
                    var slideMasterPart = slideLayoutPart.SlideMasterPart;

                    var textSlideLayout = slideLayoutPart.SlideLayout.Descendants<DocumentFormat.OpenXml.Drawing.Text>().First();
                    Console.WriteLine(textSlideLayout);

                    var textSlideMaster = slideMasterPart.SlideMaster.Descendants<DocumentFormat.OpenXml.Drawing.Text>().First();
                    Console.WriteLine(textSlideMaster);
                }
            }
        }
    }
}
