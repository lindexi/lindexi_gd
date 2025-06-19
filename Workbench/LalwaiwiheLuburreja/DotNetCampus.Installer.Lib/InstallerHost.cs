using DotNetCampus.Installer.Lib.EnvironmentCheckers;
using DotNetCampus.Installer.Lib.Hosts;
using DotNetCampus.Installer.Lib.SplashScreens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DotNetCampus.Installer.Lib;

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

    }
}
