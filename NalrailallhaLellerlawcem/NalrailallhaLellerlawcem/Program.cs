using System;
using System.IO;
using dotnetCampus.GitCommand;

namespace NalrailallhaLellerlawcem
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = Environment.CurrentDirectory;
            
            var dateTime = new DateTime(2018, 02, 05, 09, 16, 02);

            var random = new Random();
            var git = new Git(new DirectoryInfo(folder));
            var n = 0;

            git.Add(Path.Combine(folder, ".gitignore"));
            dateTime = dateTime.AddSeconds(random.Next(60));
            git.Commit(time: dateTime);

            foreach (var directory in Directory.GetDirectories(folder))
            {
                if (directory.Contains(".git"))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(directory,"*.csproj",SearchOption.TopDirectoryOnly))
                {
                    git.Add(file);

                    dateTime = dateTime.AddSeconds(random.Next(60));
                    git.Commit(time: dateTime);
                }

                var fileList = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);
                foreach (var file in fileList)
                {
                    dateTime = dateTime.AddSeconds(random.Next(60));

                    while (dateTime.Hour < 8 || dateTime.Hour >= 12)
                    {
                        dateTime = dateTime.AddSeconds(random.Next(3600));
                    }

                    git.Add(file);
                    git.Commit(time: dateTime);

                    if (random.Next(1000) == 1)
                    {
                        var tag = $"1.0.{n}";
                        n++;
                        git.Tag(tag);
                    }
                }
            }
        }
    }
}
