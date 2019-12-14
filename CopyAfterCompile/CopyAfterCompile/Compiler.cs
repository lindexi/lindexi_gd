using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CopyAfterCompile
{
    class Compiler : ICompiler
    {
        public DirectoryInfo Directory { get; }

        /// <inheritdoc />
        public Compiler(DirectoryInfo directory)
        {
            Directory = directory;
        }

        public void Compile()
        {
            Console.WriteLine($"开始编译");
            Console.WriteLine(Command("dotnet build"));
        }

        private string Command(string str)
        {
            // string str = Console.ReadLine();
            //System.Console.InputEncoding = System.Text.Encoding.UTF8;//乱码

            Process p = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = Directory.FullName,
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