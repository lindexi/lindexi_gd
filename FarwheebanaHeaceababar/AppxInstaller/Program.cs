using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using Directory = System.IO.Directory;

namespace AppxInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir;

            if (args.Length == 1)
            {
                dir = args[0];
            }
            else
            {
                dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                dir = Path.Combine(dir, "../../../../");
                dir = Path.GetFullPath(dir);

                dir = Path.Combine(dir,
                    @"FarwheebanaHeaceababar\AppPackages\FarwheebanaHeaceababar_1.0.1.0_Debug_Test\");
            }

            if (!Directory.Exists(dir))
            {
                Console.WriteLine("找不到需要安装的应用文件夹，请确定发布了 FarwheebanaHeaceababar 应用");
            }

            InstallApp(dir);
        }

        private static void InstallApp(string appFolder)
        {
            var windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("请使用管理员权限运行");
                return;
            }

            var cerFile = GetCerFile(appFolder);
            var bundleFile = GetBundle(appFolder);

            InstallCer(cerFile, appFolder);
            InstallBundle(bundleFile, appFolder);
        }

        private static void InstallBundle(string bundleFile, string appFolder)
        {
            var powerShell = GetPowerShell();

            var command = $" Add-AppxPackage  -Path \"{bundleFile}\"";
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = powerShell,
                Arguments = command,
                Verb = "runas",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = appFolder
            };

            var process = Process.Start(processStartInfo);
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            process.WaitForExit();
        }

        private static string GetPowerShell()
        {
            return "PowerShell.exe";
        }

        private static void InstallCer(string cerFile, string appFolder)
        {
            var command = $" -addstore TrustedPeople \"{cerFile}\"";
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "certutil.exe",
                Arguments = command,
                RedirectStandardOutput = true,
                Verb = "runas",
                WorkingDirectory = appFolder,
                UseShellExecute = false,
            };

            var process = new Process()
            {
                StartInfo = processStartInfo
            };
            process.Start();
            var processStandardOutput = process.StandardOutput;
            Console.WriteLine(processStandardOutput.ReadToEnd());

            process.WaitForExit();
        }

        private static string GetBundle(string appFolder)
        {
            var bundleFile = Directory.GetFiles(appFolder, "*.msixbundle").FirstOrDefault();

            if (!File.Exists(bundleFile))
            {
                Console.WriteLine("找不到 msixbundle 安装文件");
            }

            return bundleFile;
        }

        private static string GetCerFile(string appFolder)
        {
            var cerFile = Directory.GetFiles(appFolder, "*.cer").FirstOrDefault();
            if (!File.Exists(cerFile))
            {
                Console.WriteLine("找不到 cer 证书文件");
            }

            return cerFile;
        }
    }
}