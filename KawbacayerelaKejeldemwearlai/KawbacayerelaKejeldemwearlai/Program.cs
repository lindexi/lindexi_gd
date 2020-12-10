using System;
using System.IO;

namespace KawbacayerelaKejeldemwearlai
{
    class Program
    {
        static void Main(string[] args)
        {
            var filePath = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe";

            try
            {
                var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, bufferSize: 1024, FileOptions.None);
                fileStream.Dispose();
                using var stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                using var stream = File.Open("1.txt", FileMode.Append, FileAccess.Read, FileShare.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
