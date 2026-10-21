using System.Runtime.InteropServices;

namespace XiaoXiIme.ImeInterop;

/// <summary>Read-only Win32 target identity and coordinate operations.</summary>
public static class InputTargetMethods
{
    /// <summary>Reads a window-object property; properties are removed by Windows when that object is destroyed.</summary>
    [DllImport("user32.dll", EntryPoint = "GetPropW", CharSet = CharSet.Unicode)]
    public static extern nint GetWindowProperty(nint window, string name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(nint window);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(nint window, ref Point point);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LogicalToPhysicalPointForPerMonitorDPI(nint window, ref Point point);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);

    /// <summary>Converts target client coordinates to physical screen pixels, independent of caller DPI awareness.</summary>
    public static bool TryGetPhysicalPoint(nint window, int x, int y, out Point point)
    {
        point = new Point { X = x, Y = y };
        return IsWindow(window) && ClientToScreen(window, ref point)
            && LogicalToPhysicalPointForPerMonitorDPI(window, ref point);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct GuiThreadInfo
{
    public uint Size;
    public uint Flags;
    public nint Active;
    public nint Focus;
    public nint Capture;
    public nint MenuOwner;
    public nint MoveSize;
    public nint Caret;
    public Rect CaretRect;
}
