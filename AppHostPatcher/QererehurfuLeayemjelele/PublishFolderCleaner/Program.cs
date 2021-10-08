using System;
using System.Diagnostics;
using System.IO;
using dotnetCampus.Cli;

namespace PublishFolderCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Debugger.Launch();

            var options = CommandLine.Parse(args).As<Options>();

            const string libFolderName = "lib";
            var libFolder = Path.GetFullPath(Path.Combine(options.PublishFolder, libFolderName));
            var tempFolder = Path.GetFullPath(Path.Combine(options.PublishFolder, "..", Path.GetRandomFileName()));
            Directory.Move(options.PublishFolder, tempFolder);
            Directory.CreateDirectory(options.PublishFolder);
            Directory.Move(tempFolder, libFolder);
        }
    }

    public class Options
    {
        [Option('p', "PublishFolder")]
        public string PublishFolder { set; get; } = null!;

        [Option('a', "ApplicationName")]
        public string ApplicationName { set; get; } = null!;
    }
}
