using System;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace JeawufawelluChunerlecachear
{
    class Program
    {
        static void Main(string[] args)
        {
            using var wordprocessingDocument = WordprocessingDocument.Open("Test.docx", isEditable: true, new OpenSettings()
            {
                AutoSave = false
            });

            var rootPart = (MainDocumentPart) wordprocessingDocument.RootPart;
            var document = rootPart!.Document;
            var paragraph = document.Body!.GetFirstChild<Paragraph>();
            var run = paragraph!.GetFirstChild<Run>();
            var text = run!.GetFirstChild<Text>();
            text!.Text = "逗比";
        }
    }
}