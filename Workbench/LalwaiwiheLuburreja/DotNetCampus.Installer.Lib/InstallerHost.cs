using DotNetCampus.Installer.Lib.Commandlines;
using DotNetCampus.Installer.Lib.EnvironmentCheckers;
using DotNetCampus.Installer.Lib.Hosts;
using DotNetCampus.Installer.Lib.SplashScreens;
using DotNetCampus.Installer.Lib.Utils;
using DotNetCampus.InstallerSevenZipLib.DirectoryArchives;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using DotNetCampus.Installer.Lib.Hosts.Contexts;

namespace DotNetCampus.Installer.Lib;

#pragma warning disable CA1416 // 执行版本有 YY-Thunks 保底，不适用文档描述的要求版本号

public class InstallerHost
{
    public static InstallerHostBuilder CreateBuilder()
    {
        return new InstallerHostBuilder();
    }

    public InstallerHost(InstallerHostConfiguration configuration)
    {
        _configuration = configuration;
    }

    private readonly InstallerHostConfiguration _configuration;

    public int Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            return -1;
        }

        var environmentCheckResult = EnvironmentChecker.CheckEnvironment();
        switch (environmentCheckResult.ResultType)
        {
            case EnvironmentCheckResultType.Passed:
                // 正常的 dotnet core 依赖环境正常
                break;
            case EnvironmentCheckResultType.FailWithOsTooOld:
                PInvoke.MessageBox(HWND.Null, $"不支持 Win7 以下系统，当前系统版本 {Environment.OSVersion}", "系统版本过低", MESSAGEBOX_STYLE.MB_OK);
                return -1;
            case EnvironmentCheckResultType.FailWithMissingPatch:
                // 后续可以考虑在这里帮助安装补丁
                // 安装完成之后需要重启，重启最好写入到注册表的 RunOnce 里面，这样大部分杀毒软件都不会拦截安装包在重启之后重新运行
                // 注册表的 RunOnce 路径是 HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\RunOnce
                PInvoke.MessageBox(HWND.Null, $"系统环境异常，缺少 KB2533623 补丁", "系统环境异常", MESSAGEBOX_STYLE.MB_OK);
                return -1;
            case EnvironmentCheckResultType.FailWithUnknownError:
                PInvoke.MessageBox(HWND.Null, $"环境检测出现未知错误", "安装失败", MESSAGEBOX_STYLE.MB_OK);
                return -1;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (_configuration.SplashScreenFile is not null)
        {
            ShowSplashScreen();
        }
        else
        {
            Install(IntPtr.Zero);
        }

        return 0;
    }

    private void ShowSplashScreen()
    {
        if (_configuration.SplashScreenFile == null)
        {
            return;
        }

        var splashScreen = new SplashScreen(_configuration.SplashScreenFile);

        splashScreen.Showed += (_, eventArgs) =>
        {
            // 等待欢迎界面启动完成了，再继续执行后续代码，确保欢迎窗口足够快显示
            var thread = new Thread(() =>
            {
                try
                {
                    Install(eventArgs.SplashScreenWindowHandler);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            })
            {
                IsBackground = false,// 需要是前台窗口，确保主线程退出之后，当前工作线程依然还能继续运行。只要有一个线程还在运行，进程就不会退出。为什么需要这样做？因为很多应用软件管理器都会依靠其调起的安装包进程是否退出来判断是否安装完成
            };
            thread.Start();
        };

        splashScreen.Show();
    }

    private void Install(IntPtr splashScreenWindowHandler)
    {
        var workingFolder = _configuration.WorkingFolder;
        string installerApplicationFile;

#if DEBUG
        var debugInstallerFile =
            @"..\..\..\..\DotNetCampus.Installer.Sample\bin\Debug\net9.0-windows\DotNetCampus.Installer.Sample.exe";
        debugInstallerFile = Path.GetFullPath(debugInstallerFile);
        if (File.Exists(debugInstallerFile))
        {
            installerApplicationFile = debugInstallerFile;
        }
        else
#endif
        {
            installerApplicationFile = ExtractInstallerAssets();
        }

        List<string> argumentList =
        [
            InstallOptions.VerbName,// verb

            // 传入当前安装包启动器的 PID 也许安装包界面程序有用
            $"--{InstallOptions.BoostPidOptionName}",
            Environment.ProcessId.ToString(),
        ];

        if (splashScreenWindowHandler != IntPtr.Zero)
        {
            // 传入欢迎界面的句柄，安装包会在安装界面开始时欢迎界面
            argumentList.Add($"--{InstallOptions.SplashScreenWindowHandlerOptionName}");
            argumentList.Add(splashScreenWindowHandler.ToInt64().ToString());
        }

        var processStartInfo = new ProcessStartInfo(installerApplicationFile, argumentList);
        var context = new ProcessStartInfoConfigurationContext()
        {
            ProcessStartInfo = processStartInfo,
            WorkingFolder = workingFolder,
            SplashScreenWindowHandler = splashScreenWindowHandler
        };
        _configuration.InstallerProcessStartConfigAction?.Invoke(context);

        var process = Process.Start(processStartInfo)!;
        process.WaitForExit();

        // 尝试清理工作文件夹
        FolderDeleteHelper.DeleteFolder(workingFolder.FullName);

        Environment.Exit(process.ExitCode);
    }

    private string ExtractInstallerAssets()
    {
        var workingFolder = _configuration.WorkingFolder;
        var installerResourceAssetsInfo = _configuration.InstallerResourceAssetsInfo;
        using var assetsStream = installerResourceAssetsInfo.GetManifestResourceStream();
        var resourceAssetsFolder = Directory.CreateDirectory(Path.Join(workingFolder.FullName, installerResourceAssetsInfo.ManifestResourceName));
        DirectoryArchive.Decompress(assetsStream, resourceAssetsFolder);

        // 带界面的安装包界面程序
        var installerApplicationFile = Path.Join(resourceAssetsFolder.FullName, _configuration.InstallerRelativePath);

        if (!File.Exists(installerApplicationFile))
        {
            throw new FileNotFoundException(
                $"无法找到 {installerResourceAssetsInfo.ManifestResourceName} 里的 {_configuration.InstallerRelativePath} 安装器文件", installerApplicationFile);
        }

        return installerApplicationFile;
    }
}

