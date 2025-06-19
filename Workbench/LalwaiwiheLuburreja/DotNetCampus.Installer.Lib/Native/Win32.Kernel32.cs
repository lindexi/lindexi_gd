using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Installer.Lib.Native;

public partial class Win32
{
    public static class Kernel32
    {
        public const string LibraryName = "kernel32";

        [DllImport(LibraryName, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(IntPtr modulePtr);
    }
}
