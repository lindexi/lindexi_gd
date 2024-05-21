using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using Windows.Foundation;
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
        // 不需要调用了，因为在 UNO 底层已经调用
        //// 这句话不能调用多次 虽然调用多次不会炸
        // https://tronche.com/gui/x/xlib/display/XInitThreads.html
        // It is only necessary to call this function if multiple threads might use Xlib concurrently. If all calls to Xlib functions are protected by some other access mechanism (for example, a mutual exclusion lock in a toolkit or through explicit client programming), Xlib thread initialization is not required. It is recommended that single-threaded programs not call this function.
        //XInitThreads();
        //XInitThreads();
        //XInitThreads();
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

    [MemberNotNull(nameof(_x11InkWindow))]
    public void Start(Window unoWindow)
    {
        var type = unoWindow.GetType();
        var nativeWindowPropertyInfo = type.GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var x11Window = nativeWindowPropertyInfo!.GetMethod!.Invoke(unoWindow, null)!;
        // Uno.WinUI.Runtime.Skia.X11.X11Window
        var x11WindowType = x11Window.GetType();

        var x11WindowIntPtr = (IntPtr) x11WindowType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!.Invoke(x11Window, null)!;
        
        try
        {
            if (X11PlatformThreading == null)
            {
                X11PlatformThreading = new X11PlatformThreading(X11Info);
                X11PlatformThreading.Run();
            }
            
            var x11InkWindow = new X11InkWindow(X11Info, x11WindowIntPtr, X11PlatformThreading);
            _x11InkWindow = x11InkWindow;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void Draw(Point position)
    {
        EnsureStart();
        _x11InkWindow.Draw(position);
    }

    private X11PlatformThreading? X11PlatformThreading { get; set; }

    private X11InkWindow? _x11InkWindow;

    private IntPtr X11InkWindowIntPtr
    {
        get
        {
            EnsureStart();
            return _x11InkWindow.X11InkWindowIntPtr;
        }
    }

    [MemberNotNull(nameof(_x11InkWindow))]
    private void EnsureStart()
    {
        if (_x11InkWindow is null)
        {
            throw new InvalidOperationException();
        }
    }
}

class X11InkWindow
{
    public X11InkWindow(X11Info x11Info, IntPtr mainWindowHandle, X11PlatformThreading x11PlatformThreading)
    {
        X11PlatformThreading = x11PlatformThreading;
        _x11Info = x11Info;
        _mainWindowHandle = mainWindowHandle;
        var display = x11Info.Display;
        var rootWindow = x11Info.RootWindow;
        var screen = x11Info.Screen;

        var xDisplayWidth = XDisplayWidth(display, screen);
        var xDisplayHeight = XDisplayHeight(display, screen);

        XMatchVisualInfo(display, screen, 32, 4, out var info);
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
            colormap = XCreateColormap(display, rootWindow, visual, 0),
            border_pixel = 0,
            background_pixel = new IntPtr(0x65565656),
        };

        var childWindowHandle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref xSetWindowAttributes);
        
        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XSelectInput(display, childWindowHandle, mask);

        // 设置不接受输入
        var region = XCreateRegion();
        XShapeCombineRegion(display, childWindowHandle, ShapeInput, 0, 0, region, ShapeSet);
        
        XSetTransientForHint(display, childWindowHandle, mainWindowHandle);

        XMapWindow(display, childWindowHandle);

        X11InkWindowIntPtr = childWindowHandle;
    }
    
    private X11PlatformThreading X11PlatformThreading { get; }
    
    private readonly X11Info _x11Info;
    private readonly IntPtr _mainWindowHandle;

    public IntPtr X11InkWindowIntPtr { get; }
    
    public void Draw(Point position)
    {
        
    }
    
    private Task InvokeAsync(Action action)
    {
       return X11PlatformThreading.InvokeAsync(action, X11InkWindowIntPtr);
    }
}
