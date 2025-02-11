using System.Linq;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Presentation;
using NonVisualDrawingProperties = DocumentFormat.OpenXml.Presentation.NonVisualDrawingProperties;
using NonVisualShapeProperties = DocumentFormat.OpenXml.Presentation.NonVisualShapeProperties;
using TextBody = DocumentFormat.OpenXml.Presentation.TextBody;

var file = @"C:\lindexi\Document\CeargecereculagaiRearharkechaqekai.pptx";
using var presentationDocument =
    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(file, false);
var presentationPart = presentationDocument.PresentationPart;
var slidePart = presentationPart!.SlideParts.First();
var slide = slidePart.Slide;
var textBody = slide.Descendants<TextBody>().First();
var paragraph = textBody.GetFirstChild<Paragraph>()!;
DocumentFormat.OpenXml.Drawing.ParagraphProperties paragraphProperties = paragraph.ParagraphProperties!;
var leftMargin = paragraphProperties.LeftMargin;
var rightMargin = paragraphProperties.RightMargin;
var indent = paragraphProperties.Indent;