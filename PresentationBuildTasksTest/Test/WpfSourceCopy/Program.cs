using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WpfSourceCopy
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            folder = Path.Combine(folder!, @"..\..\..");
            var presentationBuildTasksCsprojFile =
                Path.Combine(folder, @"..\..\WPF\PresentationBuildTasks\PresentationBuildTasks.csproj");
            var csproj = File.ReadAllText(presentationBuildTasksCsprojFile);

            const string wpfSourceDir = @"E:\程序\ethylene156\God\wpf_3.1\src\Microsoft.DotNet.Wpf\src\";
            string copyFolder = Path.GetFullPath("WpfSourceDir");

            var regex = new Regex(@"\$\(WpfSourceDir\)\\(\S*\.cs)", RegexOptions.Compiled);
            foreach (var line in csproj.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(temp => !string.IsNullOrEmpty(temp))
                .Select(temp => temp.Replace("\r", ""))
                .Where(temp => !string.IsNullOrEmpty(temp)))
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    var filePath = match.Groups[1].Value;
                    var file = Path.Combine(wpfSourceDir, filePath);
                    if (File.Exists(file))
                    {
                        var copyFile = Path.Combine(copyFolder, filePath);
                        var directory = Path.GetDirectoryName(copyFile);
                        Directory.CreateDirectory(directory!);
                        File.Copy(file, copyFile);
                    }
                    else
                    {

                    }
                }
            }
        }
    }
}
