using System;
using System.IO;
using dotnetCampus.GitCommand;

namespace CopyAfterCompile
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = "1.txt";
            if (File.Exists(file))
            {
                var str = File.ReadAllText("1.txt");
            }
            var directory = new DirectoryInfo(KonairlerecufeeCealemwowa.Directory);

            var git = new Git(directory);
            var logCommit = git.GetLogCommit();

        }
    }

    class KonairlerecufeeCealemwowa
    {
        public const string Directory = "../../../../../";
    }
}
