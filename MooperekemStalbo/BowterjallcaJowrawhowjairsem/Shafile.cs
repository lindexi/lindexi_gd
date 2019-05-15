using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;

namespace MooperekemStalbo.Controllers
{
    public static class Shafile
    {
        public static bool Verify(FileStream stream, string sha)
        {
            return GetFile(stream) == sha;
        }

        public static string GetFile(FileStream stream)
        {
            using (var sha = SHA256.Create())
            {
                stream.Seek(0, SeekOrigin.Begin);

                var file = ByteToString(sha.ComputeHash(stream));

                stream.Seek(0, SeekOrigin.Begin);

                return file;
            }
        }

        public static string ByteToString(byte[] byteList)
        {
            return string.Concat(byteList.Select(temp => temp.ToString("x2")));
        }
    }
}