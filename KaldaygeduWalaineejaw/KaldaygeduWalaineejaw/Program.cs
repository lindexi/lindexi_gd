using System;
using System.Collections.Generic;
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

            var dataSpaces = DataSpaces.Load(cf);
            var encryptionInfo = EncryptionInfo.Load(cf);

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


    // 注意: 生成的代码可能至少需要 .NET Framework 4.5 或 .NET Core/Standard 2.0。


    // ReSharper disable once InconsistentNaming

    // ReSharper disable once InconsistentNaming


    // UNICODE-LP-P4

    // UTF-8-LP-P4
}
