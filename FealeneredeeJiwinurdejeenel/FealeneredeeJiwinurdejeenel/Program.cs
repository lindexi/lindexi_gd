using System;
using System.IO;
using Microsoft.DotNet.CodeFormatting;

namespace FealeneredeeJiwinurdejeenel
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = new FileInfo(@"..\..\..\..\CearfilunemNalnogeenayaiwhem\Program.cs");
            if (file.Exists)
            {
                var fileFormatChecker = new FileFormatChecker(file);
                fileFormatChecker.FormatFile();
            }
        }
    }

    public class FileFormatChecker
    {
        public FileFormatChecker(FileInfo file)
        {
            File = file;
        }

        public FileInfo File { get; }

        /// <summary>
        /// 尝试格式化文件
        /// </summary>
        public void FormatFile()
        {
            var formattingEngine = FormattingEngine.Create();

        }
    }
}
