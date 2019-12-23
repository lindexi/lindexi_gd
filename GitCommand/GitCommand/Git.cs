using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnetCampus.GitCommand
{
    public class Git
    {
        static Git()
        {
#if NETCOREAPP
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        /// <inheritdoc />
        public Git(DirectoryInfo repo)
        {
            if (ReferenceEquals(repo, null)) throw new ArgumentNullException(nameof(repo));
            if (!Directory.Exists(repo.FullName))
            {
                // 为什么不使用 repo.Exits 因为这个属性默认没有刷新，也就是在创建 DirectoryInfo 的时候文件夹不存在，那么这个值就是 false 即使后续创建了文件夹也不会刷新，需要调用 Refresh 才可以刷新，但是 Refresh 需要修改很多属性
                throw new ArgumentException("必须传入存在的文件夹", nameof(repo));
            }

            Repo = repo;


        }

        /// <summary>
        /// 两个版本修改的文件
        /// </summary>
        /// <param name="source">可以传入commit或分支</param>
        /// <param name="target">可以传入commit或分支</param>
        public List<GitDiffFile> DiffFile(string source, string target)
        {
            var gitDiffFileList = new List<GitDiffFile>();

            return gitDiffFileList;
        }

        public string[] GetLogCommit()
        {
            var file = Path.GetTempFileName();
            Control($"log --pretty=format:\"%h\" > {file}");

            return File.ReadAllLines(file);
        }

        public string[] GetLogCommit(string formCommit, string toCommit)
        {
            var file = Path.GetTempFileName();
            Control($"log --pretty=format:\"%h\" {formCommit}..{toCommit} > {file}");

            return File.ReadAllLines(file);
        }
        public void Clone(string repoUrl)
        {
            Control($"clone {repoUrl}");
        }

        public static Git Clone(string repoUrl, DirectoryInfo directory)
        {
            var command = $"clone {repoUrl} \"{directory.FullName}\"";
            Console.WriteLine(command);

            var git = @"C:\Program Files\Git\bin\git.exe";
            if (!File.Exists(git))
            {
                git = "git";
            }

            var processStartInfo = new ProcessStartInfo(git, command);
            var process = Process.Start(processStartInfo);
            process.WaitForExit((int)TimeSpan.FromMinutes(10).TotalMilliseconds);

            return new Git(directory);
        }

        public void Clean()
        {
            Control("clean -xdf");
        }

        public void FetchAll()
        {
            Control("fetch --all");
        }

        public DirectoryInfo Repo { get; }

        private const string GitStr = "git ";

        private string Control(string str)
        {
            str = FileStr() + str;
            WriteLog(str);
            str = Command(str,Repo.FullName);

            WriteLog(str);
            return str;
        }

        private static void WriteLog(string str)
        {
            Console.WriteLine(str);
        }

        private string FileStr()
        {
            return string.Format(GitStr, Repo.FullName);
        }

        private static string Command(string str,string workingDirectory)
        {
            // string str = Console.ReadLine();
            //System.Console.InputEncoding = System.Text.Encoding.UTF8;//乱码

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false, //是否使用操作系统shell启动
                    RedirectStandardInput = true, //接受来自调用程序的输入信息
                    RedirectStandardOutput = true, //由调用程序获取输出信息
                    RedirectStandardError = true, //重定向标准错误输出
                    CreateNoWindow = true, //不显示程序窗口
                    StandardOutputEncoding = Encoding.GetEncoding("GBK") //Encoding.UTF8
                    //Encoding.GetEncoding("GBK");//乱码
                }
            };

            p.Start(); //启动程序

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(str + "&exit");

            p.StandardInput.AutoFlush = true;
            //p.StandardInput.WriteLine("exit");
            //向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
            //同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令

            bool exited = false;

            //// 超时
            //Task.Run(() =>
            //{
            //    Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ =>
            //    {
            //        if (exited)
            //        {
            //            return;
            //        }

            //        try
            //        {
            //            if (!p.HasExited)
            //            {
            //                Console.WriteLine($"{str} 超时");
            //                p.Kill();
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            Console.WriteLine(e);
            //        }
            //    });
            //});

            //获取cmd窗口的输出信息
            string output = p.StandardOutput.ReadToEnd();
            //Console.WriteLine(output);
            output += p.StandardError.ReadToEnd();
            //Console.WriteLine(output);

            //StreamReader reader = p.StandardOutput;
            //string line=reader.ReadLine();
            //while (!reader.EndOfStream)
            //{
            //    str += line + "  ";
            //    line = reader.ReadLine();
            //}

            p.WaitForExit(TimeSpan.FromMinutes(1).Milliseconds); //等待程序执行完退出进程
            p.Close();

            exited = true;

            return output + "\r\n";
        }

        public void Checkout(string commit)
        {
            Control($"checkout {commit}");
        }

        /// <summary>
        /// 创建新分支
        /// </summary>
        /// <param name="branchName"></param>
        public void CheckoutNewBranch(string branchName)
        {
            Control($"checkout -b {branchName}");
        }
    }

    public class GitDiffFile
    {
        /// <inheritdoc />
        public GitDiffFile(DiffType diffType, FileInfo file)
        {
            DiffType = diffType;
            File = file;
        }

        public DiffType DiffType { get; }
        public FileInfo File { get; }
    }

    public enum DiffType
    {
        Added,// A
        Copied,// C
        Deleted,// D
        Modified,// M
        Renamed,// R
        Changed,// T
        Unmerged,// U
        Unknown,// X
    }
}