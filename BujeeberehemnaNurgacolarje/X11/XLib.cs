using System.Runtime.InteropServices;

namespace BlankX11App.X11;
internal static class XLib
{
    private const string libX11 = "libX11.so.6";

    [DllImport(libX11)]
    public static extern int XDefaultScreen(nint display);

    [DllImport(libX11)]
    public static extern nint XOpenDisplay(nint display);

    [DllImport(libX11)]
    public static extern nint XRootWindow(nint display, int screen_number);

    [DllImport(libX11)]
    public static extern nint XCreateColormap(nint display, nint window, nint visual, int create);

    [DllImport(libX11)]
    public static extern nint XCreateWindow(nint display, nint parent, int x, int y, int width, int height,
        int border_width, int depth, int xclass, nint visual, nuint valuemask,
        ref XSetWindowAttributes attributes);

    [DllImport(libX11)]
    public static extern int XMapWindow(nint display, nint window);

    [DllImport(libX11)]
    public static extern int XUnmapWindow(nint display, nint window);

    [DllImport(libX11)]
    public static extern int XFlush(nint display);

    [DllImport(libX11)]
    public static extern int XDestroyWindow(nint display, nint window);

    [DllImport(libX11)]
    public static extern nint XNextEvent(nint display, out XEvent xevent);
}
