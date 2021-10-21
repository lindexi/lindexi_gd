using System;
using System.Diagnostics;
using System.IO;

namespace QarnafahayWalllukerrairbar
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = Process.GetCurrentProcess().MainModule.FileName;
            DelayDeleteFile(fileName, 2);
        }

        private static void DelayDeleteFile(string fileName, int delaySecond = 2)
        {
            fileName = Path.GetFullPath(fileName);
            var folder = Path.GetDirectoryName(fileName);
            var currentProcessFileName = Path.GetFileName(fileName);

            var arguments = $"/c timeout /t {delaySecond} && DEL /f {currentProcessFileName} ";

            var processStartInfo = new ProcessStartInfo()
            {
                Verb = "runas", // 如果程序是管理员权限，那么运行 cmd 也是管理员权限
                FileName = "cmd",
                UseShellExecute = false,
                CreateNoWindow = true, // 如果需要隐藏窗口，设置为 true 就不显示窗口
                Arguments = arguments,
                WorkingDirectory = folder,
            };

            Process.Start(processStartInfo);
        }
    }
}
