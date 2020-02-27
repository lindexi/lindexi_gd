using DocumentFormat.OpenXml.Packaging;
using System;
using System.Diagnostics;
using DocumentFormat.OpenXml.Presentation;

namespace WhocohefurWallqemwaychurgu
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var presentationDocument = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(@"E:\lindexi\测试.pptx", false))
            {
                var presentationPart = presentationDocument.PresentationPart;
                var presentation = presentationPart.Presentation;

                // 先获取页面
                var slideIdList = presentation.SlideIdList;

                foreach (var slideId in slideIdList.ChildElements.OfType<SlideId>())
                {
                    // 获取页面内容
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId);

                    foreach (var paragraph in
                        slidePart.Slide
                            .Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                    {
                        // 获取段落
                        // 在 PPT 文本是放在形状里面
                        foreach (var text in
                            paragraph.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                        {
                            // 获取段落文本，这样不会添加文本格式
                            Debug.WriteLine(text.Text);
                        }
                    }
                }
            }
        }
    }
}
