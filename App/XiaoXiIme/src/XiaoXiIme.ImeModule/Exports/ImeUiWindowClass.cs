using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule;

internal static unsafe class ImeUiWindowClass
{
    internal const uint ApplyInputMessage = 0x8001;

    internal static nint CreateDispatchWindow() => EnsureRegistered()
        ? CreateWindowEx(0, ImeExportsContract.ImeUiClassName, null, 0, 0, 0, 0, 0, new nint(-3), 0, s_module, 0)
        : 0;

    private static nint s_module;
    private const uint ClassIme = 0x00010000;
    private const uint ClassVerticalRedraw = 0x00000001;
    private const uint ClassHorizontalRedraw = 0x00000002;
    private const uint GetModuleHandleExFlagFromAddress = 0x00000004;
    private const uint GetModuleHandleExFlagUnchangedRefCount = 0x00000002;
    private const int ErrorClassAlreadyExists = 1410;

    [UnmanagedCallersOnly(EntryPoint = "UIWindowProcedure", CallConvs = [typeof(CallConvStdcall)])]
    private static nint UIWindowProcedure(nint window, uint message, nuint wParam, nint lParam)
    {
        try
        {
            if (message == ApplyInputMessage)
            {
                AsyncImmInput.ApplyPending(window);
                return 0;
            }

            // IMM may deliver activation through the UI window instead of the activation export.
            if (message == 0x0281)
            {
                var context = nint.Size == 4 ? GetWindowLong(window, 0) : GetWindowLongPtr(window, 0);
                if (context != 0)
                {
                    ImeExports.ImeSetActiveContextManaged(context, wParam != 0 ? 1 : 0);
                }
            }

            return DefWindowProc(window, message, wParam, lParam);
        }
        catch (Exception error)
        {
            ImeLifecycleDiagnostics.RecordBoundaryFailure(nameof(UIWindowProcedure), error);
            return 0;
        }
    }

    internal static bool EnsureRegistered()
    {
        var windowProcedure = (nint)(delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nint>)&UIWindowProcedure;
        if (!GetModuleHandleEx
            (
                GetModuleHandleExFlagFromAddress | GetModuleHandleExFlagUnchangedRefCount,
                windowProcedure,
                out var module
            ))
        {
            return false;
        }

        s_module = module;
        fixed (char* className = ImeExportsContract.ImeUiClassName)
        {
            var windowClass = new WindowClassEx
            {
                Size = (uint)sizeof(WindowClassEx),
                Style = ClassIme | ClassVerticalRedraw | ClassHorizontalRedraw,
                WindowProcedure = windowProcedure,
                ExtraWindowBytes = nint.Size * 2,
                Instance = module,
                ClassName = className,
            };

            return RegisterClassEx(&windowClass) != 0 || Marshal.GetLastPInvokeError() == ErrorClassAlreadyExists;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowClassEx
    {
        internal uint Size;
        internal uint Style;
        internal nint WindowProcedure;
        internal int ExtraClassBytes;
        internal int ExtraWindowBytes;
        internal nint Instance;
        internal nint Icon;
        internal nint Cursor;
        internal nint BackgroundBrush;
        internal char* MenuName;
        internal char* ClassName;
        internal nint SmallIcon;
    }

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleExW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetModuleHandleEx(uint flags, nint moduleName, out nint module);

    [DllImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    private static extern ushort RegisterClassEx(WindowClassEx* windowClass);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true)]
    private static extern int GetWindowLong(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", ExactSpelling = true)]
    private static extern nint GetWindowLongPtr(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(uint exStyle, string className, string? title, uint style, int x, int y,
        int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
    private static extern nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);
}