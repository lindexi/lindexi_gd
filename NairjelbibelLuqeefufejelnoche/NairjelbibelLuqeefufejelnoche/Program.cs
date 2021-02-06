using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace NairjelbibelLuqeefufejelnoche
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"f:\temp\压缩格式不对的课件.pptx";
            var tempFolder = @"f:\temp\zip压缩格式不对的课件";
            var newZipFile = @"f:\temp\压缩格式不对的课件 new.pptx";

            try
            {
                using (var presentationDocument =
                    DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(file, false))
                {

                }
            }
            catch (InvalidDataException e)
            {
                Console.WriteLine(e);
            }

            var folder = tempFolder;

            using (var zipFile = Ionic.Zip.ZipFile.Read(file))
            {
                zipFile.ExtractAll(folder);
            }

            // 重新压缩回
            System.IO.Compression.ZipFile.CreateFromDirectory(folder, newZipFile);

            using (var presentationDocument =
                DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(newZipFile, false))
            {

            }
        }
    }
}
