using System.Reflection;
using System.Runtime.Versioning;

using CPF.Linux;

using static CPF.Linux.XFixes;
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;
using Microsoft.UI;

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

    public void Start(Window unoWindow)
    {
        var type = unoWindow.GetType();
        var nativeWindowPropertyInfo = type.GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var x11Window = nativeWindowPropertyInfo!.GetMethod!.Invoke(unoWindow, null)!;
        // Uno.WinUI.Runtime.Skia.X11.X11Window
        var x11WindowType = x11Window.GetType();

        var x11WindowIntPtr = (IntPtr) x11WindowType.GetProperty("Window", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!.Invoke(x11Window, null)!;

        var x11InkWindow = new X11InkWindow(X11Info, x11WindowIntPtr);
    }

    public void Draw()
    {

    }
}

record X11Info(IntPtr Display, int Screen, IntPtr RootWindow)
{

}

class X11InkWindow
{
    public X11InkWindow(X11Info x11Info, IntPtr mainWindowHandle)
    {
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
        
        XMapWindow(display, childWindowHandle);

        _x11InkWindowIntPtr = childWindowHandle;
    }

    private readonly X11Info _x11Info;
    private readonly IntPtr _mainWindowHandle;

    private readonly IntPtr _x11InkWindowIntPtr;

    public void Run()
    {
        _eventsThread = new Thread(RunInner)
        {
            Name = $"X11InkWindow XEvents {Interlocked.Increment(ref _threadCount) - 1}",
            IsBackground = true
        };

        _eventsThread.Start();
    }

    private void RunInner()
    {
        var display = _x11Info.Display;
        while (true)
        {
            var xNextEvent = XNextEvent(display, out var @event);
            if (@event.type == XEventName.Expose)
            {

            }
            else if (@event.type == XEventName.ClientMessage)
            {
                var clientMessageEvent = @event.ClientMessageEvent;
                if (clientMessageEvent.message_type == 0 && clientMessageEvent.ptr1 == _invokeMessageId)
                {
                    List<Action> tempList;
                    lock (_invokeList)
                    {
                        tempList = _invokeList.ToList();
                        _invokeList.Clear();
                    }
                    
                    foreach (var action in tempList)
                    {
                        action();
                    }
                }
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private readonly List<Action> _invokeList = new List<Action>();
    private readonly IntPtr _invokeMessageId = new IntPtr(123123123);

    public async Task InvokeAsync(Action action)
    {
        var taskCompletionSource = new TaskCompletionSource();
        lock (_invokeList)
        {
            _invokeList.Add(() =>
            {
                action();
                taskCompletionSource.SetResult();
            });
        }

        // 在 Avalonia 里面，是通过循环读取的方式，通过 XPending 判断是否有消息
        // 如果没有消息就进入自旋判断是否有业务消息和判断是否有 XPending 消息
        // 核心使用 epoll_wait 进行等待
        // 在 UNO 里面，也是和 Avalonia 差不多的方法，加上 XConnectionNumber 的方式，用于进行等待
        // 如果有消息进入，则立刻可以返回
        // 只是差别只是 UNO 让 X11 线程作为非主线程，所有 UI 逻辑不和 X11 线程混淆。而 Avalonia 让 X11 线程作为主线程，所有处理逻辑都在相同的主线程处理，主线程同时执行所有 UI 逻辑
        // 整个逻辑比较复杂
        // 这里简单处理，只通过发送 ClientMessage 的方式，告诉消息循环需要处理业务逻辑
        // 发送 ClientMessage 是一个合理的方式，根据官方文档说明，可以看到这是没有明确定义的
        // https://www.x.org/releases/X11R7.5/doc/man/man3/XClientMessageEvent.3.html
        // https://tronche.com/gui/x/xlib/events/client-communication/client-message.html
        // The X server places no interpretation on the values in the window, message_type, or data members.
        // 在 cpf 里面，和 Avalonia 实现差不多，也是在判断 XPending 是否有消息，没消息则判断是否有业务逻辑
        // 最后再进入等待逻辑。似乎 CPF 这样的方式会导致 CPU 占用略微提升
        var @event = new XEvent
        {
            ClientMessageEvent =
            {
                type = XEventName.ClientMessage,
                send_event = true,
                window = _x11InkWindowIntPtr,
                message_type = 0,
                format = 32,
                ptr1 = _invokeMessageId,
                ptr2 = 0,
                ptr3 = 0,
                ptr4 = 0,
            }
        };
        XSendEvent(_x11Info.Display, _x11InkWindowIntPtr, false, 0, ref @event);

        XFlush(_x11Info.Display);

        await taskCompletionSource.Task;
    }

    private Thread? _eventsThread;
    private static int _threadCount;
}
