using System;
using System.IO;

namespace FealeneredeeJiwinurdejeenel
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public class FileFormatChecker
    {
        public FileFormatChecker(FileInfo file)
        {
            File = file;
        }

        public FileInfo File { get; }
    }
}
