using System;
using System.IO;
using DocumentFormat.OpenXml.Packaging;

namespace LichearheecalJalewhujulo
{
    class Program
    {
        static void Main(string[] args)
        {
            using var fileStream =
                new FileStream("1.pptx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            var openSettings = new OpenSettings()
            {
                RelationshipErrorHandlerFactory = RelationshipErrorHandler.CreateRewriterFactory(Rewriter)
            };

            using (var presentationDocument =
                DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(fileStream, true, openSettings))
            {
            }
        }

        static string Rewriter(Uri partUri, string id, string uri)
            => $"http://unknown?id={id}";
    }
}