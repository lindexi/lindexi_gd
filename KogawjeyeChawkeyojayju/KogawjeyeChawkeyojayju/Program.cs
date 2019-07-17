using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace KogawjeyeChawkeyojayju
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("开启程序");
            Console.WriteLine("开始读取配置");

            var xmlSerializer = new XmlSerializer(typeof(RunFile[]));
            var file = new FileStream("配置.xml", FileMode.Open);

            RunFile[] runFileList = null;
            try
            {
                using (file)
                {
                    runFileList = (RunFile[]) xmlSerializer.Deserialize(file);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("读取文件失败" + e);
            }

            if (runFileList == null || runFileList.Length == 0)
            {
                Console.WriteLine("没有读取到需要运行程序");
                Console.Read();
                return;
            }

            Console.WriteLine("开始启动程序");

            foreach (var runFile in runFileList)
            {
                Console.WriteLine("开始启动" + runFile.Name);

                if (!string.IsNullOrEmpty(runFile.ProcessName))
                {
                    Console.WriteLine("开始寻找是否有重复的进程");
                    bool canFind = false;
                    foreach (var temp in Process.GetProcesses())
                    {
                        if (temp.ProcessName.Contains(runFile.ProcessName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Console.WriteLine("找到" + temp.ProcessName + "包含配置的" + runFile.ProcessName + "所以不进行启动");
                            canFind = true;
                            break;
                        }
                    }

                    if (canFind)
                    {
                        continue;
                    }
                }

                if (runFile.File.Equals("cmd", StringComparison.InvariantCultureIgnoreCase))
                {
                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        Arguments = " /k " + runFile.Argument,
                        CreateNoWindow = false,
                        RedirectStandardError = false,
                        UseShellExecute = true,
                    };

                    Process.Start(processStartInfo);
                }
                else
                {
                    if (File.Exists(runFile.File))
                    {
                        var processStartInfo = new ProcessStartInfo()
                        {
                            FileName = runFile.File,
                            Arguments = runFile.Argument,
                            CreateNoWindow = false,
                            RedirectStandardError = false,
                            RedirectStandardInput = false,
                            RedirectStandardOutput = false,
                            UseShellExecute = true,
                            WorkingDirectory = Path.GetDirectoryName(runFile.File),
                        };

                        Process.Start(processStartInfo);
                    }
                    else
                    {
                        Console.WriteLine("找不到需要运行的文件" + runFile.File);
                    }
                }

                Console.WriteLine("启动" + runFile.Name + "完成");
            }

            Console.Read();
        }
    }
}