using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

            //var fromBase64String = Convert.FromBase64String("dehYIJ5fJ/8ykdnFaPPn7g==");
            //var text = Encoding.UTF8.GetString(fromBase64String);

            // SHA512
            var sha512 = SHA512.Create();

            // saltValue="0CxQlkIZI3Z5dQWea+KLMw=="
            var saltBase64 = "0CxQlkIZI3Z5dQWea+KLMw==";
            var saltByteList = Convert.FromBase64String(saltBase64);
            var key = "123";
            var keyByteList = Encoding.Unicode.GetBytes(key);

            var h0 = sha512.ComputeHash(saltByteList.Concat(keyByteList).ToArray());

            var hn = h0;
            var iterator = 0x00000000;
            var spinCount = 100000;
            for (; iterator < spinCount; iterator++)
            {
                var iteratorByteList = BitConverter.GetBytes(iterator);
                hn = sha512.ComputeHash(iteratorByteList.Concat(hn).ToArray());
            }

            var hf = sha512.ComputeHash(hn.Concat(new byte[]{0x5f, 0xb2, 0xad, 0x01, 0x0c, 0xb9, 0xe1, 0xf6 }).ToArray());


          
        }

        private static bool CheckOfficeDocumentWithPassword(FileInfo file)
        {
            CompoundFile cf = new CompoundFile(file.FullName);

            var dataSpaces = DataSpaces.Load(cf);
            var encryptionInfo = EncryptionInfo.Load(cf);
            var encryptedPackage = EncryptedPackage.Load(cf);


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
