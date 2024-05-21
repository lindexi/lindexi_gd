using System.Reflection;
using System.Runtime.Versioning;

using CPF.Linux;

using static CPF.Linux.XFixes;
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;

namespace UnoInk.X11Ink;

[SupportedOSPlatform("Linux")]
internal class X11InkProvider
{
    public X11InkProvider()
    {
        // 这句话不能调用多次
        XInitThreads();
        var display = XOpenDisplay(IntPtr.Zero);
        var screen = XDefaultScreen(display);

        if (XCompositeQueryExtension(display, out var eventBase, out var errorBase) == 0)
        {
            Console.WriteLine("Error: Composite extension is not supported");
            XCloseDisplay(display);
            throw new NotSupportedException("Error: Composite extension is not supported");
            return;
        }
        else
        {
            //Console.WriteLine("XCompositeQueryExtension");
        }

        var rootWindow = XDefaultRootWindow(display);

        var x11Info = new X11Info(display, screen, rootWindow);
        X11Info = x11Info;
    }

    public X11Info X11Info { get; }

    public void Start(Window unoWindow)
    {
        var type = unoWindow.GetType();
        var nativeWindowPropertyInfo = type.GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var x11WindowWrapper = nativeWindowPropertyInfo!.GetMethod!.Invoke(unoWindow, null)!;

        var x11WindowWrapperType = x11WindowWrapper.GetType();

        var x11WindowIntPtr = (IntPtr) x11WindowWrapperType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!.Invoke(x11WindowWrapper, null)!;

    }

    public void Draw()
    {

    }
}

record X11Info(IntPtr Display, int Screen, IntPtr RootWindow);
