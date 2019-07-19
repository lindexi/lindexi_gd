using System;
using System.Diagnostics;
using System.Text;

namespace JalniqejallQaneehemfu
{
    class Program
    {
        static void Main(string[] args)
        {
            // 请自己创建项目，然后替换下面两个值
            var repo = @"d:\lindexi\NolekekanuYemrobemjaywo\";
            var commit = "b81c6cdd0514a2bc643e1ae94398ec91fd0ab11d";

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            AutoBuild(repo, commit);
        }

        private static void AutoBuild(string repo, string commit)
        {
            var git = new Git(repo);
            git.ShowLog();
            git.Checkout(commit);
            git.Clean();

            var nuGet = new NuGet(repo);
            nuGet.Restore();

            var dotNet = new DotNet(repo);
            dotNet.Restore();

            var msbuild = new Msbuild(repo);
            msbuild.Build();
        }
    }

    class DotNet
    {
        /// <inheritdoc />
        public DotNet(string repo)
        {
            Repo = repo;
        }

        public string Repo { get; }

        public void Restore()
        {
            Command.Control(" dotnet restore ", Repo);
        }

    }

    class Msbuild
    {
        /// <inheritdoc />
        public Msbuild(string repo)
        {
            Repo = repo;
        }

        public string Repo { get; }

        public void Build()
        {
            Command.Control("msbuild /m ", Repo);
        }
    }

    class NuGet
    {
        /// <inheritdoc />
        public NuGet(string repo)
        {
            Repo = repo;
        }

        public string Repo { get; }

        public void Restore()
        {
            Command.Control("nuget restore");
        }
    }

    static class Command
    {
        public static string Control(string str, string workingDirectory = null)
        {
            // string str = Console.ReadLine();
            //System.Console.InputEncoding = System.Text.Encoding.UTF8;//乱码

            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true; //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true; //重定向标准错误输出
            p.StartInfo.CreateNoWindow = true; //不显示程序窗口
            p.StartInfo.StandardOutputEncoding = /*Encoding.UTF8; //*/Encoding.GetEncoding("GBK");//乱码
            if (workingDirectory != null)
            {
                p.StartInfo.WorkingDirectory = workingDirectory;
            }

            p.Start(); //启动程序

            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(str + "&exit");

            p.StandardInput.AutoFlush = true;
            //p.StandardInput.WriteLine("exit");
            //向标准输入写入要执行的命令。这里使用&是批处理命令的符号，表示前面一个命令不管是否执行成功都执行后面(exit)命令，如果不执行exit命令，后面调用ReadToEnd()方法会假死
            //同类的符号还有&&和||前者表示必须前一个命令执行成功才会执行后面的命令，后者表示必须前一个命令执行失败才会执行后面的命令


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

            p.WaitForExit(); //等待程序执行完退出进程
            p.Close();

            Console.WriteLine(output);
            return output + "\r\n";
        }
    }

    class Git
    {
        /// <inheritdoc />
        public Git(string repo)
        {
            Repo = repo;
        }

        public string Repo { get; }

        public void ShowLog()
        {
            Control("log");
        }

        public void Checkout(string commit)
        {
            Control(" reset .");
            Control(" checkout .");
            Control(" checkout " + commit);
        }

        public void Clean()
        {
            Console.WriteLine(Control(" clean -xdf"));
        }

        private string _gitStr = "git -C {0}";

        private string FileStr()
        {
            return string.Format(_gitStr, Repo) + " ";
        }

        private string Control(string str)
        {
            str = FileStr() + str;

            return Command.Control(str);
        }
    }
}