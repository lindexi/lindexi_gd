using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using dotnetCampus.Configurations;

namespace CopyAfterCompile
{
    /// <summary>
    /// 编译器
    /// </summary>
    internal class Compiler : ICompiler
    {
        private readonly ILogger _logger;
        public IAppConfigurator AppConfigurator => CopyAfterCompile.AppConfigurator.GetAppConfigurator();

        public CompileConfiguration CompileConfiguration => AppConfigurator.Of<CompileConfiguration>();

        /// <inheritdoc />
        public Compiler(ILogger logger)
        {
            _logger = logger;
            var fileSniff = new FileSniff();
            fileSniff.Sniff();
        }

        private void Log(string str) => _logger?.Info(str);

        public virtual void Compile()
        {
            Log($"开始编译");
            Log(Command("dotnet restore"));
            Log(Command("msbuild /m /p:configuration=\"release\""));
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="str"></param>
        /// <param name="workingDirectory">工作路径默认为代码文件夹</param>
        /// <returns></returns>
        protected string Command(string str, string workingDirectory = "")
        {
            // string str = Console.ReadLine();
            //System.Console.InputEncoding = System.Text.Encoding.UTF8;//乱码

            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = CompileConfiguration.CodeDirectory;
            }

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


            return output + "\r\n";
        }
    }
}