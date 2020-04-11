using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace NallwerewawchailawileeForeehakel
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"F:\lindexi\1.pptx";
            GetOleObjects(file);
        }

        public static void GetOleObjects(string pptxFilePath)
        {
            using (var doc = PresentationDocument.Open(pptxFilePath, false))
            {
                // 我假定你只有一个页面
                var slidePart = doc.PresentationPart.SlideParts.First();

                // 拿到页面
                var slide = slidePart.Slide;

                // 找到所有也许是 ole 的元素
                foreach (var frame in slide.CommonSlideData.ShapeTree
                    .OfType<DocumentFormat.OpenXml.Presentation.GraphicFrame>())
                {
                    var oleElement = frame.Descendants<DocumentFormat.OpenXml.Presentation.OleObject>()
                        .FirstOrDefault();
                    if (oleElement != null)
                    {
                        TryGetFallbackImage(oleElement);
                    }
                }
            }
        }

        private static (bool isSuccess, FileInfo file) TryGetFallbackImage(OleObject oleElement)
        {
            var slide = oleElement.Ancestors<Slide>().First();

            var slidePart = slide.SlidePart;
            var frame = oleElement.Ancestors<GraphicFrame>().First();

            var frameGraphic = frame.Graphic.GraphicData;

            var fallback = frameGraphic.Descendants<DocumentFormat.OpenXml.AlternateContentFallback>().FirstOrDefault();
            if (fallback == null)
            {
                return (false, null);
            }

            var picture = fallback.Descendants<DocumentFormat.OpenXml.Presentation.Picture>().FirstOrDefault();

            if (picture == null)
            {
                return (false, null);
            }

            var embed = picture.BlipFill.Blip.Embed.Value;

            var part = slidePart.GetPartById(embed);

            if (part is ImagePart imagePart)
            {
                if (imagePart.ContentType == "image/x-wmf" || imagePart.ContentType == "image/x-emf")
                {
                    var stream = part.GetStream(FileMode.Open, FileAccess.Read);
                    var fileName = Path.GetFileName(imagePart.Uri.OriginalString);
                    var file = Path.Combine(@"F:\lindexi", fileName);
                    File.WriteAllBytes(file, ReadAllBytes(stream));

                    return (true, new FileInfo(file));
                }
            }

            return (false, null);
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}