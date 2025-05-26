// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Win32;

Console.WriteLine($"Hello, World!测试中文 RuntimeIdentifier={RuntimeInformation.RuntimeIdentifier} FrameworkDescription={RuntimeInformation.FrameworkDescription} OSVersion={Environment.OSVersion}");

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
            Console.WriteLine("Either running on Win8+, or KB2533623 is installed");
        }
        else
        {
            Console.WriteLine("Likely running on Win7 or older OS, and KB2533623 is not installed");
        }
    }
}

var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DotNetCampus.Installer.Boost.Assets.zip")!;
Console.WriteLine(stream.Length);
//var assetsZip = Path.Join(AppContext.BaseDirectory, "Assets.zip");
//using var fileStream = new FileStream(assetsZip,FileMode.Create,FileAccess.Write);
//stream.CopyTo(fileStream);