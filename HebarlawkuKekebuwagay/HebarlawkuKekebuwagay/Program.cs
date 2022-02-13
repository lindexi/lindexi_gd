
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HebarlawkuKekebuwagay
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Fx {Environment.CurrentDirectory}");

            if (args.Length > 0)
            {
                Console.Read();
                return;
            }

            var location = Assembly.GetExecutingAssembly().Location;
            var fileName = Path.GetFileNameWithoutExtension(location);
            var directory = Path.GetDirectoryName(location);

            //var testFolder = new DirectoryInfo("Test2");
            //Directory.CreateDirectory(testFolder.FullName);
            //Environment.CurrentDirectory = testFolder.FullName;
            Environment.CurrentDirectory = @"I:\";

            var exe = Path.Combine(directory, fileName + ".exe");
            var processStartInfo = new ProcessStartInfo(exe, "fx")
            {
                // net framework 炸掉
                // net core 啥都没发生，使用 I:\ 作为路径
                UseShellExecute = true,
            };
            var process = Process.Start(processStartInfo);
            process.WaitForExit();
            // System.ComponentModel.Win32Exception: 'An error occurred trying to start process' 目录名称无效
            // The directory name is invalid
            // [c# - Win32Exception: The directory name is invalid - Stack Overflow](https://stackoverflow.com/questions/990562/win32exception-the-directory-name-is-invalid )
        }
    }
}