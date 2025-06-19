// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using DotNetCampus.Installer.Boost;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using DotNetCampus.Installer.Lib;

//Console.WriteLine($"Hello, World!测试中文 RuntimeIdentifier={RuntimeInformation.RuntimeIdentifier} FrameworkDescription={RuntimeInformation.FrameworkDescription} OSVersion={Environment.OSVersion}");

if (!OperatingSystem.IsWindows())
{
    return;
}

using (var hModule = PInvoke.LoadLibrary("Kernel32.dll"))
{
    if (!hModule.IsInvalid)
    {
        IntPtr hFarProc = PInvoke.GetProcAddress(hModule, "AddDllDirectory");
        if (hFarProc != IntPtr.Zero)
        {
            // 正常的 dotnet core 依赖环境正常
            Console.WriteLine("Success. Either running on Win8+, or KB2533623 is installed");
        }
        else
        {
            Console.WriteLine("Likely running on Win7 or older OS, and KB2533623 is not installed");

            // [操作系统版本 - Win32 apps Microsoft Learn](https://learn.microsoft.com/zh-cn/windows/win32/sysinfo/operating-system-version )
            if (Environment.OSVersion.Version < new Version(6, 1))
            {
                PInvoke.MessageBox(HWND.Null, $"不支持 Win7 以下系统，当前系统版本 {Environment.OSVersion}", "系统版本过低", MESSAGEBOX_STYLE.MB_OK);

                return;
            }
            else
            {
                // 后续可以决定在这里帮助安装补丁。当前还没做，就直接退出
                return;
            }
        }
    }
}

var splashScreen = new SplashScreen(AssemblyAssetsHelper.GetTempSplashScreenImageFile());

splashScreen.Showed += (_, eventArgs) =>
{
    // 等待欢迎界面启动完成了，再继续执行后续代码，确保欢迎窗口足够快显示
    var thread = new Thread(() =>
    {
        try
        {
            Install(eventArgs);
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