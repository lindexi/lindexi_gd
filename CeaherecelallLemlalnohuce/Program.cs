using CPF.Linux;

namespace BujeeberehemnaNurgacolarje;

public class Program
{
    static void Main(string[] args)
    {
        XLib.XInitThreads();
        var display = XLib.XOpenDisplay(0);
        var screen = XLib.XDefaultScreen(display);
        var defaultScreen = XLib.XDefaultScreen(display);
        var rootWindow = XLib.XRootWindow(display, defaultScreen);
        XLib.XMatchVisualInfo(display, screen, 32, 4, out var info);
        var visual = info.visual;
        var valueMask = SetWindowValuemask.BackPixmap
                        | SetWindowValuemask.BackPixel
                        | SetWindowValuemask.BorderPixel
                        | SetWindowValuemask.BitGravity
                        | SetWindowValuemask.WinGravity
                        | SetWindowValuemask.BackingStore
                        | SetWindowValuemask.ColorMap;
        var attr = new XSetWindowAttributes
        {
            backing_store = 1,
            bit_gravity = Gravity.NorthWestGravity,
            win_gravity = Gravity.NorthWestGravity,
            override_redirect = false,  // 参数：_overrideRedirect
            colormap = XLib.XCreateColormap(display, rootWindow, visual, 0),
        };

        var handle = XLib.XCreateWindow(display, rootWindow, 100, 100, 320, 240, 0,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref attr);
        XLib.XMapWindow(display, handle);
        XLib.XFlush(display);

        while (XLib.XNextEvent(display, out var xEvent) == default)
        {
        }

        XLib.XUnmapWindow(display, handle);
        XLib.XDestroyWindow(display, handle);
    }
}