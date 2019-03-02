using System;
using System.IO;
using System.IO.Compression;

namespace MooperekemStalbo.Controllers
{
    public class MedaltraFairjousuFowluNererisMoubeturce
    {
        public void CheckFile(FileInfo file)
        {
            ZipFile.ExtractToDirectory(file.FullName, Path.Combine(Folder, "temp", Guid.NewGuid().ToString()));
        }

        public string Folder { get; set; }
    }
}