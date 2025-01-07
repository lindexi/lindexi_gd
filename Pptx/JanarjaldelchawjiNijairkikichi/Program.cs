// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

var file = "KaylerrurdedobifelKerereleakur.docx";
using var wordprocessingDocument = WordprocessingDocument.Open(file,isEditable:true);
MainDocumentPart? mainDocumentPart = wordprocessingDocument.MainDocumentPart;
foreach (OpenXmlElement openXmlElement in mainDocumentPart?.Document?.Body ?? [])
{
    if (openXmlElement is DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
    {
        if (paragraph.ParagraphProperties is {} paragraphProperties)
        {
            SpacingBetweenLines? spacingBetweenLines = paragraphProperties.SpacingBetweenLines;
            if (spacingBetweenLines?.LineRule?.Value == LineSpacingRuleValues.Exact)
            {
                var lineValue = spacingBetweenLines.Line?.Value;
            }
        }
    }
}

Console.WriteLine("Hello, World!");
