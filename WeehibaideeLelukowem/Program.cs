using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WeehibaideeLelukowem
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(AreEqual(
                new DirectoryInfo("C:\\Scratch"),
                new DirectoryInfo("C:\\Scratch\\")));

            Console.WriteLine(AreEqual(
                new DirectoryInfo("C:\\Windows\\Microsoft.NET\\Framework"),
                new DirectoryInfo("C:\\Windows\\Microsoft.NET\\Framework\\v3.5\\1033\\..\\..")));

            Console.WriteLine(AreEqual(
                new DirectoryInfo("C:\\Scratch\\"),
                new DirectoryInfo("C:\\Scratch\\4760\\..\\..")));

            Console.WriteLine("Press ENTER to continue");
            Console.ReadLine();
        }

        private static bool AreEqual(DirectoryInfo dir1, DirectoryInfo dir2)
        {
            return AreEqual(dir1.FullName, dir2.FullName);
        }

        private static bool AreEqual(string folderPath1, string folderPath2)
        {
            folderPath1 = Path.GetFullPath(folderPath1);
            folderPath2 = Path.GetFullPath(folderPath2);

            if (folderPath1.Length == folderPath2.Length)
            {
                return string.Equals(folderPath1, folderPath2/*, StringComparison.OrdinalIgnoreCase*/);
            }
            else if (folderPath1.Length == folderPath2.Length + 1)
            {
                // folderPath1 = @"F:\temp\"
                // folderPath2 = @"F:\temp"
                return folderPath1.Contains(folderPath2 /*, StringComparison.OrdinalIgnoreCase*/);
            }
            else if (folderPath1.Length + 1 == folderPath2.Length)
            {
                // folderPath1 = @"F:\temp"
                // folderPath2 = @"F:\temp\"
                return folderPath2.Contains(folderPath1 /*, StringComparison.OrdinalIgnoreCase*/);
            }

            return false;
        }
    }
}
