using System.Runtime.InteropServices;

namespace SplashScreensLib.Native;

public partial class Win32
{
    public static class Kernel32
    {
        public const string LibraryName = "kernel32";

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(IntPtr modulePtr);
    }
}
