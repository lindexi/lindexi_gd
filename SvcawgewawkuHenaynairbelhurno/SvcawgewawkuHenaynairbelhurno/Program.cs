using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Windows.Win32.System.Threading;

var processList = Process.GetProcessesByName(Assembly.GetExecutingAssembly().GetName().Name);
if (processList.Length > 1)
{
    Console.WriteLine($"被启动的进程");
    Console.Read();
}

var mainModuleFileName = Process.GetCurrentProcess().MainModule!.FileName!;

var startupInfo = new STARTUPINFOW();
startupInfo.cb = (uint) Marshal.SizeOf<STARTUPINFOW>();

var arguments = "\"C:\\windows\\notepad.exe\"";

CreateProcess(mainModuleFileName, arguments, IntPtr.Zero, IntPtr.Zero, false, (uint) PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE,
    IntPtr.Zero, IntPtr.Zero, startupInfo, out var information);

Windows.Win32.PInvoke.CloseHandle(information.hProcess);
Windows.Win32.PInvoke.CloseHandle(information.hThread);

Console.Read();

[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateProcessW", ExactSpelling = true, SetLastError = true)]
static extern bool CreateProcess([In] string lpApplicationName, [In] string lpCommandLine, [In] IntPtr lpProcessAttributes,
    [In] IntPtr lpThreadAttributes, [In] bool bInheritHandles, [In] uint dwCreationFlags, [In] IntPtr lpEnvironment,
    [In] IntPtr lpCurrentDirectory, [In] in STARTUPINFOW lpStartupInfo, [Out] out PROCESS_INFORMATION lpProcessInformation);
