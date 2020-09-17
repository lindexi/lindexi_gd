using System;
using System.Data.OleDb;
using System.IO;
using OpenMcdf;

namespace KaldaygeduWalaineejaw
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"带密码的课件PPTX.pptx";

            if (CheckOfficeDocumentWithPassword(new FileInfo(file)))
            {
                Console.WriteLine("带密码");
            }
        }

        private static bool CheckOfficeDocumentWithPassword(FileInfo file)
        {
            CompoundFile cf = new CompoundFile(file.FullName);
            var numDirectories = cf.GetNumDirectories();
            for (int i = 0; i < numDirectories; i++)
            {
                var nameDirEntry = cf.GetNameDirEntry(i);

                if (cf.RootStorage.TryGetStream(nameDirEntry, out var stream))
                {
                    if (nameDirEntry == "EncryptionInfo")
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
