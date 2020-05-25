using System;
using System.IO;
using System.IO.Compression;

namespace LeardallkibalkareaJeaqenegarwe
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = @"f:\lindexi\test\";
            var fileList = Directory.GetFiles(folder);

            using var fileStream = new FileStream(@"f:\lindexi\zip\1.zip", FileMode.Create);
            fileStream.SetLength(0);

            using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);

            foreach (var file in fileList)
            {
                var zipArchiveEntry = zipArchive.CreateEntry(Path.GetFileName(file), CompressionLevel.NoCompression);

                using (var stream = zipArchiveEntry.Open())
                {
                    using var toZipStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    toZipStream.CopyTo(stream);
                }

                fileStream.Flush(true);
            }
        }
    }
}