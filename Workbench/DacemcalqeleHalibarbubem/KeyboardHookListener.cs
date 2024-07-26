using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

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
        _globalHookId = SetHook(_proc, WINDOWS_HOOK_ID.WH_KEYBOARD_LL, 0);
    }

    /// <summary>
    /// 卸载钩子
    /// </summary>
    public static void UnhookKeyboard()
    {
        //if (_threadHookId != IntPtr.Zero)
        //{
        //    PInvoke.UnhookWindowsHookEx(new HHOOK(_threadHookId));
        //}

        if (!_globalHookId.IsInvalid)
        {
            PInvoke.UnhookWindowsHookEx(new HHOOK(_globalHookId.DangerousGetHandle()));
            _globalHookId.SetHandleAsInvalid();
        }

        _proc = null;
    }

    public static event RawKeyEventHandler? KeyDown;
    public static event RawKeyEventHandler? KeyUp;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static LRESULT HookCallback(int code, WPARAM wparam, LPARAM lparam)
    {
        if (wparam == PInvoke.WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lparam);

            if (KeyDown != null)
                KeyDown(null, new RawKeyEventArgs(vkCode, false));
        }
        else if (wparam == PInvoke.WM_KEYUP)
        {
            int vkCode = Marshal.ReadInt32(lparam);

            if (KeyUp != null)
            {
                KeyUp(null, new RawKeyEventArgs(vkCode, false));
            }
        }

        return PInvoke.CallNextHookEx(_globalHookId, code, wparam, lparam);
    }

    /// <summary>
    /// 定义一个局部变量保存，防止被垃圾回收
    /// </summary>
    private static HOOKPROC? _proc;
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

    //private static IntPtr _threadHookId = IntPtr.Zero;
    private static UnhookWindowsHookExSafeHandle _globalHookId = new UnhookWindowsHookExSafeHandle(IntPtr.Zero, false);

    private static UnhookWindowsHookExSafeHandle SetHook(HOOKPROC proc, WINDOWS_HOOK_ID hookType, uint threadId)
    {
        using var currentProcess = Process.GetCurrentProcess();
        var currentProcessMainModule = currentProcess.MainModule!;

        var moduleHandle = PInvoke.GetModuleHandle(currentProcessMainModule.ModuleName);

        return PInvoke.SetWindowsHookEx(hookType, proc, moduleHandle, threadId);
    }
}