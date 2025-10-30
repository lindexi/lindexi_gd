using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

using CPF.Linux;

using DotNetCampus.Logging;

using Microsoft.UI;

using Uno.Extensions;

using static CPF.Linux.ShapeConst;
using static CPF.Linux.XLib;

namespace UnoInk.Inking.X11Platforms;

[SupportedOSPlatform("Linux")]
public class X11Window : X11WindowNativeInterop
{
    public X11Window(X11Application application, X11WindowCreateInfo createInfo) : this(application, CreateX11Window(application, createInfo))
    {
        AppendPid();
        SetNetWmWindowTypeNormal();
    }

    internal X11Window(X11Application application, IntPtr x11WindowIntPtr) : base(application.X11Info,
        x11WindowIntPtr)
    {
        Application = application;
        application.RegisterWindow(this);
    }

    public X11Application Application { get; }

    public IntPtr GC => _gc ??= XCreateGC(Display, X11WindowIntPtr, 0, 0);

    private IntPtr? _gc;

    private static IntPtr CreateX11Window(X11Application application, X11WindowCreateInfo createInfo)
    {
        var x11Info = application.X11Info;
        var display = x11Info.Display;
        var rootWindow = x11Info.RootWindow;
        var screen = x11Info.Screen;

        var width = createInfo.Width;
        var height = createInfo.Height;

        if (createInfo.IsFullScreen)
        {
            width = x11Info.XDisplayWidth;
            height = x11Info.XDisplayHeight;
        }

        //StaticDebugLogger.WriteLine($"创建窗口 {width},{height}");

        XLib.XMatchVisualInfo(display, screen, 32, 4, out var info);
        var visual = info.visual;

        var valueMask =
                //SetWindowValuemask.BackPixmap
                0
                | SetWindowValuemask.BackPixel
                | SetWindowValuemask.BorderPixel
                | SetWindowValuemask.BitGravity
                | SetWindowValuemask.WinGravity
                | SetWindowValuemask.BackingStore
                | SetWindowValuemask.ColorMap
            //| SetWindowValuemask.OverrideRedirect
            ;
        var xSetWindowAttributes = new XSetWindowAttributes
        {
            backing_store = 1,
            bit_gravity = Gravity.NorthWestGravity,
            win_gravity = Gravity.NorthWestGravity,
            //override_redirect = true, // 设置窗口的override_redirect属性为True，以避免窗口管理器的干预
            colormap = XLib.XCreateColormap(display, rootWindow, visual, 0),
            border_pixel = 0,
            background_pixel = IntPtr.Zero,
        };

        var x11Window = XLib.XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref xSetWindowAttributes);

        Log.Info($"[InkCore][X11Apps][X11Window] 创建窗口 Visual={visual} WH={width},{height} XID={x11Window}");

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XLib.XSelectInput(display, x11Window, mask);

        Log.Info($"[InkCore][X11Apps][X11Window] 完成创建窗口 X11Window={x11Window}");

        return x11Window;
    }

    internal void DispatchEvent(XEvent @event)
    {
        OnDispatchEvent(@event);
    }

    protected virtual unsafe void OnDispatchEvent(XEvent @event)
    {
        OnReceiveEvent(&@event);
    }
}
