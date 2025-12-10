using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using X11ApplicationFramework.Apps;
using X11ApplicationFramework.Natives;
using X11ApplicationFramework.Primitive;

using static X11ApplicationFramework.Natives.XLib;

namespace X11ApplicationFramework.Utils;

public static class XWindowHelper
{
    [Obsolete("不能获取全部窗口")]
    public static IReadOnlyList<XWindowId> EnumerateTopLevelWindowsViaNetClientList(this X11InfoManager x11Info)
    {
        var dpy = x11Info.Display;

        var root = XDefaultRootWindow(dpy);

        var netClientList = x11Info.X11Atoms._NET_CLIENT_LIST;

        var xaWindow = X11Atoms.XA_WINDOW; // XA_WINDOW 在 C 为 extern Atom，使用 Intern 简化

        var status = XGetWindowProperty(
            dpy, root, netClientList,
            0, 1024, false,
            xaWindow,
            out var actualType, out var actualFormat,
            out var nitemsPtr, out var bytesAfterPtr,
            out var propPtr);

        _ = actualType;
        _ = bytesAfterPtr;

        if (status != 0 || propPtr == IntPtr.Zero || actualFormat != 32)
        {
            return [];
        }

        var nitems = (ulong) nitemsPtr.ToInt64();
        var result = new XWindowId[nitems];

        // 每个项是 32-bit window ID；在 64-bit 进程需按 4 字节读取
        for (ulong i = 0; i < nitems; i++)
        {
            var w = Marshal.ReadInt32(propPtr, (int) (i * 4));
            result[i] = new XWindowId(new IntPtr(w));
        }

        XFree(propPtr);
        return result;
    }

    /// <summary>
    /// 使用 XQueryTree 获取子窗口列表
    /// </summary>
    /// <param name="x11Info"></param>
    /// <param name="windowId">如果传入空，将使用 root 窗口</param>
    /// <returns></returns>
    public static IReadOnlyList<XWindowId> EnumerateChildrenViaXQueryTree(this X11InfoManager x11Info, XWindowId? windowId = null)
    {
        var dpy = x11Info.Display;

        IntPtr window;
        if (windowId != null)
        {
            window = windowId.Value;
        }
        else
        {
            var root = XDefaultRootWindow(dpy);
            window = root;
        }
        var status = XQueryTree(dpy, window, out _, out _, out var children, out var count);

        //Console.WriteLine($"Tree Count={count}");

        if (status == 0 || children == IntPtr.Zero || count == 0) return [];

        var result = new List<XWindowId>(count);
        // children 是 Window* 数组；每项 32-bit ID
        for (uint i = 0; i < count; i++)
        {
            var w = Marshal.ReadInt32(children, (int) (i * 4));
            if (w is 0)
            {
                continue;
            }

            result.Add(new XWindowId(new IntPtr(w)));
        }
        XFree(children);
        return result;
    }

    public static string? GetWindowName(this X11InfoManager x11Info, XWindowId window)
    {
        return GetWindowName(x11Info.Display, window);
    }

    public static string? GetWindowName(IntPtr display, IntPtr window)
    {
        nint namePtr = 0;
        if (XFetchName(display, window, ref namePtr) == 0 || namePtr == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringAnsi(namePtr);
        }
        finally
        {
            XFree(namePtr);
        }
    }
}
