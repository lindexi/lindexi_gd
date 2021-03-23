using System.Runtime.InteropServices;

namespace RuhuyagayBemkaijearfear
{
    static partial class Win32
    {
        public static class Dwmapi
        {
            public const string LibraryName = "Dwmapi.dll";

            [DllImport(LibraryName, ExactSpelling = true, PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DwmIsCompositionEnabled();
        }
    }
}