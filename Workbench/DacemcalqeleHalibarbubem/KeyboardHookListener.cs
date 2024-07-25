using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace DacemcalqeleHalibarbubem;

public static class KeyboardHookListener
{
    public static void HookKeyboard()
    {
        if (_proc != null) return;
        _proc = HookCallback;
        ////安装当前线程的钩子
        //_threadHookId =
        //    InterceptKeys.SetHook(_proc, InterceptKeys.WH_KEYBOARD, GetCurrentThreadId());
        //安装全局的钩子
        _globalHookId = InterceptKeys.SetHook(_proc, InterceptKeys.WH_KEYBOARD_LL, 0);
    }

    /// <summary>
    /// 获得当前执行线程id
    /// </summary>
    /// <returns></returns>
    [DllImport("Kernel32.dll", ExactSpelling = true)]
    private static extern uint GetCurrentThreadId();
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);


    /// <summary>
    /// 卸载钩子，建议在密码框失焦时卸载钩子
    /// </summary>
    public static void UnhookKeyboard()
    {
        if (_threadHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_threadHookId);
        }

        if (_globalHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_globalHookId);
        }

        _proc = null;
    }

    public static event RawKeyEventHandler? KeyDown;
    public static event RawKeyEventHandler? KeyUp;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (wParam == (IntPtr) InterceptKeys.WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (KeyDown != null)
                KeyDown(null, new RawKeyEventArgs(vkCode, false));
        }
        else if (wParam == (IntPtr) InterceptKeys.WM_KEYUP)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (KeyUp != null)
                KeyUp(null, new RawKeyEventArgs(vkCode, false));
        }
        return CallNextHookEx(_globalHookId, nCode, wParam, lParam);
    }

    /// <summary>
    /// 定义一个局部变量保存，防止被垃圾回收
    /// </summary>
    private static LowLevelKeyboardProc? _proc;
    public delegate void RawKeyEventHandler(object? sender, RawKeyEventArgs args);
    public class RawKeyEventArgs : EventArgs
    {
        public int VKCode { get; }
        public Key Key { get; }
        public bool IsSysKey { get; }

        public RawKeyEventArgs(int VKCode, bool isSysKey)
        {
            this.VKCode = VKCode;
            this.IsSysKey = isSysKey;
            this.Key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(VKCode);
        }
    }

    private static IntPtr _threadHookId = IntPtr.Zero;
    private static IntPtr _globalHookId = IntPtr.Zero;

    static class InterceptKeys
    {
        /// <summary>
        /// 全局钩子定义
        /// </summary>
        public const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// 私有钩子定义
        /// </summary>
        public const int WH_KEYBOARD = 2;

        public static int WM_KEYDOWN = 0x0100;
        public static int WM_KEYUP = 0x0101;

        public static IntPtr SetHook(LowLevelKeyboardProc proc, int hookType, uint threadId)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(hookType, proc, GetModuleHandle(curModule?.ModuleName),
                        threadId);
                }
            }
        }
    }
}