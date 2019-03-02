using System;
using System.IO;
using System.IO.Compression;

namespace MooperekemStalbo.Controllers
{
    public class MedaltraFairjousuFowluNererisMoubeturce
    {
        public void CheckFile(FileInfo file)
        {
            // 如果文件夹不为空
            if (!Directory.Exists(Folder))
            {
                Folder = Path.GetTempPath();
            }

            ZipFile.ExtractToDirectory(file.FullName, Path.Combine(Folder, "temp", Guid.NewGuid().ToString()));
        }

        public string Folder { get; set; }
    }
}