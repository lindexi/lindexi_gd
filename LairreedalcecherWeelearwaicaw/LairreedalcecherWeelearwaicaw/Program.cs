using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

if (args.Length == 0)
{
    var securityAttributes = new SECURITY_ATTRIBUTES()
    {
        nLength = (uint) Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
        // 指定这个对象的句柄可以被子进程继承
        bInheritHandle = true
    };
    // 创建一个匿名对象的锁
    var handle = PInvoke.CreateMutex(securityAttributes, true, null);

    // 关闭对象的可继承功能，启动的子进程无法拿到锁
    PInvoke.SetHandleInformation(handle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, 0);

    // 启动子进程，将句柄传给子进程
    var mainModuleFileName = Process.GetCurrentProcess().MainModule!.FileName!;

    var startupInfo = new STARTUPINFOW();
    startupInfo.cb = (uint) Marshal.SizeOf<STARTUPINFOW>();

    var arguments = $"\"{mainModuleFileName}\" {handle.DangerousGetHandle().ToInt64()}";

    CreateProcess(null!, arguments, IntPtr.Zero, IntPtr.Zero, bInheritHandles: true, (uint) PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE,
        IntPtr.Zero, IntPtr.Zero, startupInfo, out var information);

    PInvoke.CloseHandle(information.hProcess);
    PInvoke.CloseHandle(information.hThread);

    Console.WriteLine($"按下回车释放锁");
    Console.Read();

    PInvoke.ReleaseMutex(handle);
    // 这里的 CloseHandle 和 ReleaseMutex 是不同的，调用 ReleaseMutex 是释放锁，其他的等待逻辑可以继续
    // 调用 CloseHandle 是减少句柄的引用计数，表示当前进程不再使用此句柄的对象，调用之后的此句柄不再可用
    // 和调用 handle.DangerousRelease(); 是等价的。在 Microsoft.Win32.SafeHandles.SafeFileHandle 的 DangerousRelease 方法里面也是调用 Kernal32 的 CloseHandle 方法
    PInvoke.CloseHandle(new HANDLE(handle.DangerousGetHandle()));
}
else
{
    // 这是子进程进入的分支代码

    // 子进程被传入了 Mutex 句柄的地址，由于句柄被设置了继承，因此可以在子进程拿到句柄对应的对象
    // 在 Windows 上的一个设计是在子进程的句柄表里面，继承的句柄的值是相同的，这就意味着相同的一个内核对象进行标识的句柄是完全相同的
    Console.WriteLine($"被启动的进程，开始等待锁释放 {args[0]}");

    var handle = new HANDLE(new IntPtr(long.Parse(args[0])));
    PInvoke.WaitForSingleObject(handle, unchecked((uint)Timeout.Infinite));

    Console.WriteLine($"锁被释放 {Marshal.GetLastPInvokeError()} {Marshal.GetLastPInvokeErrorMessage()}");
    Console.Read();
}

Console.Read();

[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateProcessW", ExactSpelling = true, SetLastError = true)]
static extern bool CreateProcess([In] string lpApplicationName, [In] string lpCommandLine, [In] IntPtr lpProcessAttributes,
    [In] IntPtr lpThreadAttributes, [In] bool bInheritHandles, [In] uint dwCreationFlags, [In] IntPtr lpEnvironment,
    [In] IntPtr lpCurrentDirectory, [In] in STARTUPINFOW lpStartupInfo, [Out] out PROCESS_INFORMATION lpProcessInformation);
