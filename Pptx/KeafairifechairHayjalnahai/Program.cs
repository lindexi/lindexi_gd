// See https://aka.ms/new-console-template for more information

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;

var file = @"C:\lindexi\Document\1.docx";

var wordprocessingDocument = WordprocessingDocument.Open(file,false);

var document = wordprocessingDocument.MainDocumentPart!.Document;
ParagraphProperties paragraphProperties = document.Descendants<ParagraphProperties>().First();
var indentation = paragraphProperties.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Indentation>();
Console.WriteLine("Hello, World!");
