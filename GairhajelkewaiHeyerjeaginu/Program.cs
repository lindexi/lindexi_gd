using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Path = System.IO.Path;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;

namespace GairhajelkewaiHeyerjeaginu
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var presentationDocument = PresentationDocument.Open(@"小视频.pptx", false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                var slidePart = presentationPart.SlideParts.FirstOrDefault();
                var picture = slidePart.Slide.CommonSlideData.ShapeTree.OfType<Picture>().FirstOrDefault();

                var pictureProperties = picture.NonVisualPictureProperties;

                var properties = pictureProperties.NonVisualDrawingProperties;
                var videoName = properties.Name.Value;

                var videoFromFile = pictureProperties
                    .ApplicationNonVisualDrawingProperties
                    .GetFirstChild<VideoFromFile>();
                var openXmlPart =
                    (DataPartReferenceRelationship) slidePart.GetReferenceRelationship(videoFromFile.Link.Value);
                var extension = Path.GetExtension(openXmlPart.Uri.OriginalString);
                if (string.IsNullOrEmpty(videoName))
                {
                    videoName = Path.GetFileNameWithoutExtension(openXmlPart.Uri.OriginalString);
                }
                var videoFile = videoName + extension;

                var stream = openXmlPart.DataPart.GetStream();
                var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), videoFile);
                File.WriteAllBytes(file, ReadAllBytes(stream));
            }
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