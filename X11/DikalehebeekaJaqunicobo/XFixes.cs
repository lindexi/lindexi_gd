using System.Runtime.InteropServices;

namespace CPF.Linux;

public unsafe static class XFixes
{
    [DllImport("libXfixes.so.3")]
    public static extern void XFixesSelectCursorInput(IntPtr display, IntPtr window, long mask);

    [DllImport("libXfixes.so.3")]
    public static extern IntPtr XFixesGetCursorImage(IntPtr display);

    [DllImport("libXfixes.so.3")]
    public static extern IntPtr XFixesCreateRegion(IntPtr display, IntPtr rectangles, int nrectangles);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesDestroyRegion(IntPtr display, IntPtr region);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesSetCursorName(IntPtr display, IntPtr cursor, string name);

    [DllImport("libXfixes.so.3")]
    public static extern IntPtr XFixesGetCursorName(IntPtr display, IntPtr cursor);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesChangeCursor(IntPtr display, IntPtr window, IntPtr cursor);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesHideCursor(IntPtr display, IntPtr window);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesShowCursor(IntPtr display, IntPtr window);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesSetCursorImage(IntPtr display, IntPtr cursor, ref XFixesCursorImage cursor_image);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesSetWindowShapeRegion(IntPtr display, IntPtr window, int shape_type, int x_offset,
        int y_offset, IntPtr region);

    [DllImport("libXfixes.so.3")]
    public static extern void XFixesDestroyCursor(IntPtr display, IntPtr cursor);
}

[StructLayout(LayoutKind.Sequential)]
public struct XFixesSelectionNotifyEvent
{
    public int type;
    public ulong serial;
    public bool send_event;
    public IntPtr display;
    public IntPtr window;
    public int subtype;
    public IntPtr owner;
    public IntPtr selection;
    public Time timestamp;
    public Time selection_timestamp;
}

[StructLayout(LayoutKind.Sequential)]
public struct XFixesCursorNotifyEvent
{
    public int type;
    public ulong serial;
    public bool send_event;
    public IntPtr display;
    public IntPtr window;
    public int subtype;
    public ulong cursor_serial;
    public Time timestamp;
    public Atom cursor_name;
}

[StructLayout(LayoutKind.Sequential)]
public struct XFixesCursorImage
{
    public short x, y;
    public ushort width, height;
    public ushort xhot, yhot;
    public ulong cursor_serial;

    public IntPtr pixels;
    // Additional fields for XFixes version >= 2:
    // public Atom atom;
    // public string name;
}

public struct Time
{
    public int Value;
}

[StructLayout(LayoutKind.Sequential)]
public struct XFixesCursorImageAndName
{
    public short x, y;
    public ushort width, height;
    public ushort xhot, yhot;
    public ulong cursor_serial;
    public IntPtr pixels;
    public Atom atom;
    public string name;
}