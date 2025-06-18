// See https://aka.ms/new-console-template for more information

using DotNetCampus.Installer.Boost;

using Microsoft.DotNet.Archive;

using System.Reflection;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using DotNetCampus.Installer.Boost.Microsoft.DotNet.Archive.DirectoryArchives;

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
    var workingFolder = Path.Join(Path.GetTempPath(), $"Installer_{Path.GetRandomFileName()}");
    Directory.CreateDirectory(workingFolder);

    Console.WriteLine($"Working folder: {workingFolder}");

    const string assetsFileName = "Assets.7z";
    var manifestResourceName = $"DotNetCampus.Installer.Boost.{assetsFileName}";
    var assetsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestResourceName);

    if (assetsStream is null)
    {
        throw new ArgumentException($"传入的 manifestResourceName={manifestResourceName} 找不到资源。可能是忘记嵌入资源，也可能是改了名字忘记改这里");
    }

    var testInputZipFolder = @"C:\lindexi\Work\TestZipFolder\";
    var temp7zFile = @"C:\lindexi\Input.7z";
    DirectoryArchive.Compress(new DirectoryInfo(testInputZipFolder), new FileInfo(temp7zFile));

    //using (var testInputZipFileStream = File.OpenRead(testInputZipFile))
    //using (var temp7zFileStream = new FileStream(temp7zFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
    //{
    //    var proxyInputStream = new ProxyInputStream(testInputZipFileStream);

    //    CompressionUtility.Compress(proxyInputStream, temp7zFileStream, new Progress<ProgressReport>());
    //}

    //var outputStream = new MemoryStream();
    //CompressionUtility.Decompress(assetsStream, outputStream,new Progress<ProgressReport>());

    Console.WriteLine(assetsStream.Length);
}

class ProxyInputStream : Stream
{
    public ProxyInputStream(Stream inputStream)
    {
        _inputStream = inputStream;
    }

    private readonly Stream _inputStream;

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _inputStream.Read(buffer,offset, 1);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override long Length { get; }
    public override long Position { get; set; }
}

class Fxx : Stream
{
    public override void Flush()
    {
        
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
    }

    public override bool CanRead { get; }
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length { get; }
    public override long Position { get; set; }
}