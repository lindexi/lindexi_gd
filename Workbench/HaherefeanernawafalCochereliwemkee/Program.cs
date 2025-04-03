// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.InteropServices;

if (WineDetector.IsRunningOnWine())
{
    Console.WriteLine($"在 Wine 里运行");
}
else
{
    Console.WriteLine($"不在 Wine 里运行");
}

class WineDetector
{
    public static bool IsRunningOnWine()
    {
        // https://gitlab.winehq.org/wine/wine/-/wikis/Developer-FAQ#how-can-i-detect-wine
        // If you still really want to detect Wine, check whether ntdll exports the function wine_get_version.
        try
        {
            var handle = NativeLibrary.Load("ntdll.dll");

            if (NativeLibrary.TryGetExport(handle, "wine_get_version",out var address))
            {
                Debug.Assert(address!=IntPtr.Zero);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            // 不应该发生，Windows 下 ntdll.dll 不可能不存在
            return false;
        }
    }
}