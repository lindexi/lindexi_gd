// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;
using DotNetCampus.Installer.Boost;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using DotNetCampus.Installer.Lib;
using DotNetCampus.Installer.Lib.EnvironmentCheckers;
using DotNetCampus.Installer.Lib.Hosts;
using DotNetCampus.Installer.Lib.SplashScreens;

return InstallerHost.CreateBuilder()
    .ConfigWorkingFolder(Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}")))
    .UseSplashScreen(Assembly.GetExecutingAssembly(), "DotNetCampus.Installer.Boost.SplashScreen.png")
    .BuildAndRun();

static void Install(SplashScreenShowedEventArgs eventArgs)
{
    // 准备解压缩资源文件
    var workingFolder =
    Directory.CreateDirectory(Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}"));

    Console.WriteLine($"Working folder: {workingFolder}");

    var resourceAssetsFolder = AssemblyAssetsHelper.ExtractInstallerAssetsToDirectory("Resource.assets", workingFolder);

    // 带界面的安装包界面程序
    var installerApplicationFile = Path.Join(resourceAssetsFolder.FullName, "Installer.exe");

    string[] argumentList =
    [
        "install",// verb

        // 传入欢迎界面的句柄，安装包会在安装界面开始时欢迎界面
        "--SplashScreenWindowHandler",
        eventArgs.SplashScreenWindowHandler.ToInt64().ToString(),

        // 传入当前安装包启动器的 PID 也许安装包界面程序有用
        "--BoostPid",
        Environment.ProcessId.ToString(),
    ];

    var process = Process.Start(installerApplicationFile, argumentList);
    process.WaitForExit();
    Environment.Exit(process.ExitCode);
}