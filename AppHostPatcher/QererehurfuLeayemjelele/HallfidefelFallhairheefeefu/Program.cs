using System;
using dotnetCampus.Cli;

namespace PublishFolderCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"PublishFolderCleaner {Environment.CommandLine}");
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
