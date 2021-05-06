using System;
using System.Diagnostics;

namespace KurnibalwogikidearHayhailuneyearjel
{
    class Program
    {
        static void Main(string[] args)
        {
            var batFile = "1.bat";

            var (success, output) = ExecuteCommand(batFile,null);
        }


        public static (bool success, string output) ExecuteCommand(string exeName, string arguments, string workingDirectory = "")
        {
            if (string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Environment.CurrentDirectory;
            }

            var processStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = workingDirectory,
                FileName = exeName,

                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            var process = Process.Start(processStartInfo);

            var output = process.StandardOutput.ReadToEnd();
            bool success = true;
            if (process.HasExited)
            {
                success = process.ExitCode == 0;
            }

            return (success, output);
        }
    }
}
 