using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FealeneredeeJiwinurdejeenel
{
    class Program
    {
        static void Main(string[] args)
        {
            var csporjFile = new FileInfo(@"..\..\..\..\CearfilunemNalnogeenayaiwhem\CearfilunemNalnogeenayaiwhem.csproj");
            var fileFormatChecker = new FileFormatChecker(new[]
            {
                new FileInfo(@"..\..\..\..\CearfilunemNalnogeenayaiwhem\Program.cs"),
                new FileInfo(@"..\..\..\..\CearfilunemNalnogeenayaiwhem\LogunolifiHifurcairqe.cs"),
            }, csporjFile);
            fileFormatChecker.FormatFile();
        }
    }

    public class FileFormatChecker
    {
        public FileFormatChecker(IReadOnlyList<FileInfo> fileList, FileInfo csprojFile)
        {
            FileList = fileList;
            CsprojFile = csprojFile;
        }

        public FileInfo CsprojFile { get; }

        public IReadOnlyList<FileInfo> FileList { get; }

        /// <summary>
        /// 尝试格式化文件
        /// </summary>
        public void FormatFile()
        {
            var fileList = string.Join(",", FileList.Select(file => file.FullName));

            var processStartInfo = new ProcessStartInfo("dotnet")
            {
                ArgumentList =
                {
                    "format",
                    "--workspace",
                    CsprojFile.FullName,
                    "--files",
                    fileList
                }
            };

            Process.Start(processStartInfo)?.WaitForExit();
        }
    }
}
