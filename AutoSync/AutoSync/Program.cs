using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.GitCommand;

namespace AutoSync
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder;

            if (args.Length > 0)
            {
                folder = args[0];
            }
            else
            {
                folder = Directory.GetCurrentDirectory();

                if (Directory.GetDirectories(folder).Length < 10)
                {
                    folder = Path.Combine(folder, "..\\..\\..\\..\\..\\..\\");
                    folder = Path.GetFullPath(folder);
                }
            }

            Sync(folder);

            Console.WriteLine(folder);
        }

        private static void Sync(string folder)
        {
            Console.WriteLine($"下载{folder}");
            foreach (var directory in FindGitFolder(folder))
            {
                Console.WriteLine($"更新{directory}");
                var task = Task.Run(() => { UpdateGit(directory); });
                task.Wait(TimeSpan.FromMinutes(1));
            }
        }

        private static void UpdateGit(string directory)
        {
            var git = new Git(new DirectoryInfo(directory));
            git.FetchAll();
        }

        /// <summary>
        /// 查找这个文件夹下面的所有 git 文件夹
        /// </summary>
        /// <param name="folder"></param>
        private static IEnumerable<string> FindGitFolder(string folder)
        {
            foreach (var temp in Directory.GetDirectories(folder))
            {
                // 如果这是一个 git 文件夹
                var git = Path.Combine(temp, ".git");
                if (Directory.Exists(git))
                {
                    yield return temp;
                }
                else
                {
                    foreach (var directory in FindGitFolder(temp))
                    {
                        yield return directory;
                    }
                }
            }
        }
    }

}
