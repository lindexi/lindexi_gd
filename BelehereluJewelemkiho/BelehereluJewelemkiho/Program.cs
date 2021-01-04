using System;
using System.IO;
using System.IO.Packaging;
using DocumentFormat.OpenXml.Packaging;

namespace BelehereluJewelemkiho
{
    class Program
    {
        static void Main(string[] args)
        {
            const string fileName = "Excel.xlsx";

            var openSettings = new OpenSettings()
            {
                RelationshipErrorHandlerFactory = RelationshipErrorHandler.CreateRewriterFactory(Rewriter)
            };

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (SpreadsheetDocument doc = SpreadsheetDocument.Open(fs, isEditable: true, openSettings))
                {

                }
            }
        }

        static string Rewriter(Uri partUri, string id, string uri)
            => $"http://unknown?id={id}";
    }
}
