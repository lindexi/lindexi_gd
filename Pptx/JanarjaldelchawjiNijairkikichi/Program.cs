// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;

{
    var file = "ChawchelairnelekaiWegurriqer.pptx";
    using var presentationDocument = PresentationDocument.Open(file, isEditable: true);
    SlidePart? firstSlide = presentationDocument.PresentationPart?.SlideParts.FirstOrDefault();
    if (firstSlide != null)
    {
        ShapeTree? shapeTree = firstSlide.Slide.CommonSlideData?.ShapeTree;
        foreach (OpenXmlElement treeChildElement in shapeTree?.ChildElements ?? [])
        {
            if (treeChildElement is Shape shape && shape.TextBody is { } textBody)
            {
                foreach (var paragraph in textBody.ChildElements.OfType<DocumentFormat.OpenXml.Drawing.Paragraph>())
                {
                    DocumentFormat.OpenXml.Drawing.ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
                    Int32Value? spacingPointsValue = paragraphProperties?.LineSpacing?.SpacingPoints?.Val;
                }
            }
        }
    }
}

{
    var file = "KaylerrurdedobifelKerereleakur.docx";
    using var wordprocessingDocument = WordprocessingDocument.Open(file, isEditable: true);
    MainDocumentPart? mainDocumentPart = wordprocessingDocument.MainDocumentPart;
    foreach (OpenXmlElement openXmlElement in mainDocumentPart?.Document?.Body ?? [])
    {
        if (openXmlElement is DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
        {
            if (paragraph.ParagraphProperties is { } paragraphProperties)
            {
                SpacingBetweenLines? spacingBetweenLines = paragraphProperties.SpacingBetweenLines;
                if (spacingBetweenLines?.LineRule?.Value == LineSpacingRuleValues.Exact)
                {
                    var lineValue = spacingBetweenLines.Line?.Value;
                }
            }
        }
    }
}

Console.WriteLine("Hello, World!");
