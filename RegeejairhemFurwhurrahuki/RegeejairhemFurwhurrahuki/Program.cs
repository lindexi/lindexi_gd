using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RegeejairhemFurwhurrahuki
{
    class Program
    {
        static void Main(string[] args)
        {
            List<DirectoryInfo> deviceList = FindDevice();

            foreach (var device in deviceList)
            {
                Log($"开始读取{device}盘符内容");
                try
                {
                    FindDirectory(device, 10);
                }
                catch (Exception e)
                {
                    Log(e.ToString());
                }
            }

        }

        /// <summary>
        /// 寻找文件夹
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="deep">寻找深度</param>
        private static void FindDirectory(DirectoryInfo directory, int deep)
        {
            try
            {
                Log($"开始读取 {directory.FullName} 文件夹，当前递归深度{deep}");
                deep--;
                var gitFolder = new DirectoryInfo(Path.Combine(directory.FullName, GitFolder));
                if (CheckIsGitFolder(gitFolder))
                {
                    Log($"找到{gitFolder.FullName}文件夹");
                    try
                    {
                        UpdateGitFile(directory);
                    }
                    catch (Exception e)
                    {
                        Log(e.ToString());
                    }
                }
                else
                {
                    //Log($"因为{directory.FullName}不是");
                    if (deep == 0)
                    {
                        Log($"达到递归深度{directory.FullName}不再继续寻找子文件夹");

                        return;
                    }

                    var subDirectoryList = directory.GetDirectories();

                    Log($"在{directory.FullName}找到{subDirectoryList.Length}个文件夹");

                    foreach (var temp in subDirectoryList)
                    {
                        FindDirectory(temp, deep);
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        private static void UpdateGitFile(DirectoryInfo directory)
        {
            var gitCommand = new GitCommand(directory);
            gitCommand.FetchAll();
        }

        private const string GitFolder = ".git";

        /// <summary>
        /// 判断当前是否一个 git 文件夹
        /// </summary>
        /// <param name="directory"></param>
        private static bool CheckIsGitFolder(DirectoryInfo directory)
        {
            return directory.Exists;
        }


        private static void Log(string str)
        {
            Console.WriteLine(str);

            //File.AppendAllText("log.txt", str + "\r\n");
        }


        /// <summary>
        /// 找到所有驱动器
        /// </summary>
        /// <returns></returns>
        private static List<DirectoryInfo> FindDevice()
        {
            var deviceList = new List<DirectoryInfo>();
            for (int i = 0; i < 'Z' - 'A' + 1; i++)
            {
                var device = (char)('A' + i) + ":\\";
                if (Directory.Exists(device))
                {
                    deviceList.Add(new DirectoryInfo(device));
                }
            }

            return deviceList;
        }

    }

    public class GitCommand
    {
        /// <inheritdoc />
        public GitCommand(DirectoryInfo repo)
        {
            Repo = repo;
        }

        public DirectoryInfo Repo { get; }

        public void FetchAll()
        {
            Control("fetch --all");
        }

        private string Control(string str)
        {
            str = FileStr() + str;
            Log(str);
            str = RegeejairhemFurwhurrahuki.Control.Command(str);

            Log(str);
            return str;
        }

        private static void Log(string str)
        {
            Console.WriteLine(str);
        }

        private string FileStr()
        {
            return string.Format(GitStr, Repo.FullName);
        }

        private const string GitStr = "git -C {0} ";
    }

    public static class Control
    {
        static Control()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static string Command(string str)
        {
            // string str = Console.ReadLine();
            //System.Console.InputEncoding = System.Text.Encoding.UTF8;//乱码

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,  //是否使用操作系统shell启动
                    RedirectStandardInput = true,  //接受来自调用程序的输入信息
                    RedirectStandardOutput = true,  //由调用程序获取输出信息
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

            // 超时
            Task.Run(() =>
            {
                Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ =>
                {
                    if (exited)
                    {
                        return;
                    }

                    try
                    {
                        if (!p.HasExited)
                        {
                            Console.WriteLine($"{str} 超时");
                            p.Kill();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            });

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

            exited = true;

            return output + "\r\n";
        }
    }
}