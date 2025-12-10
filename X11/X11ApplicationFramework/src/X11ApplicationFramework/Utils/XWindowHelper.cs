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
    private static IReadOnlyList<XWindowId> EnumerateTopLevelWindowsViaNetClientList(this X11InfoManager x11Info)
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
        var name = GetWindowTitleViaWMName(x11Info, window);

        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return GetWindowNameViaXFetchName(x11Info.Display, window);
    }

    public static string? GetWindowNameViaXFetchName(IntPtr display, IntPtr window)
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

    public static string? GetWindowTitleViaWMName(X11InfoManager x11Info, IntPtr window)
    {
        // 优先尝试 _NET_WM_NAME (UTF8_STRING)
        var display = x11Info.Display;
        var x11Atoms = x11Info.X11Atoms;
        var utf8String = x11Atoms.UTF8_STRING;
        var netWmName = x11Atoms._NET_WM_NAME;

        var result = XGetTextProperty(display, window, out var netProp, netWmName);
        if (result != 0 && netProp.value != 0)
        {
            try
            {
                // 当 encoding 为 UTF8_STRING 且 format==8 时按 UTF-8 解析
                if (netProp.encoding == utf8String && netProp.format == 8 && netProp.nitems > 0)
                {
                    return Marshal.PtrToStringUTF8(netProp.value);
                }
                // 其他编码可能为 COMPOUND_TEXT，尝试 ANSI 回退（不保证正确）
                if (netProp.nitems > 0)
                {
                    return Marshal.PtrToStringAnsi(netProp.value);
                }
            }
            finally
            {
                XFree(netProp.value);
            }
        }

        // 回退到传统 WM_NAME

        result = XGetWMName(display, window, out var prop);
        if (result != 0 && prop.value != 0)
        {
            try
            {
                // WM_NAME 通常为 COMPOUND_TEXT/STRING，format==8，使用 ANSI 回退
                if (prop.nitems > 0)
                {
                    // 若是 UTF8_STRING 也用 UTF8 解析
                    if (prop.encoding == utf8String && prop.format == 8)
                    {
                        return Marshal.PtrToStringUTF8(prop.value);
                    }

                    return Marshal.PtrToStringAnsi(prop.value);
                }
            }
            finally
            {
                XFree(prop.value);
            }
        }

        return null;
    }
}
