// See https://aka.ms/new-console-template for more information

using CPF.Linux;

using System;
using System.Diagnostics;
using System.Runtime;

using static CPF.Linux.XLib;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

var rootWindow = XDefaultRootWindow(display);

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
    background_pixel = 0,
};

var xDisplayWidth = XDisplayWidth(display, screen) / 2;
var xDisplayHeight = XDisplayHeight(display, screen) / 2;
var handle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);


XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);

XMapWindow(display, handle);
XFlush(display);

var white = XWhitePixel(display, screen);
var black = XBlackPixel(display, screen);

var gc = XCreateGC(display, handle, 0, 0);
XSetForeground(display, gc, white);
XSync(display, false);

var invokeList = new List<Action>();
var invokeMessageId = new IntPtr(123123123);

var n = -704351309; // 3590615987

async Task InvokeAsync(Action action)
{
    var taskCompletionSource = new TaskCompletionSource();
    lock (invokeList)
    {
        invokeList.Add(() =>
        {
            action();
            taskCompletionSource.SetResult();
        });
    }

    var manualResetEventSlim = new ManualResetEventSlim();
    var wait = manualResetEventSlim.Wait(TimeSpan.FromSeconds(1));

    // 在 Avalonia 里面，是通过循环读取的方式，通过 XPending 判断是否有消息
    // 如果没有消息就进入自旋判断是否有业务消息和判断是否有 XPending 消息
    // 核心使用 epoll_wait 进行等待
    // 整个逻辑比较复杂
    // 这里简单处理，只通过发送 ClientMessage 的方式，告诉消息循环需要处理业务逻辑
    // 发送 ClientMessage 是一个合理的方式，根据官方文档说明，可以看到这是没有明确定义的
    // https://www.x.org/releases/X11R7.5/doc/man/man3/XClientMessageEvent.3.html
    // The X server places no interpretation on the values in the window, message_type, or data members.
    // 在 cpf 里面，和 Avalonia 实现差不多，也是在判断 XPending 是否有消息，没消息则判断是否有业务逻辑
    // 最后再进入等待逻辑。似乎 CPF 这样的方式会导致 CPU 占用略微提升
    var @event = new XEvent
    {
        ClientMessageEvent =
        {
            type = XEventName.ClientMessage,
            send_event = true,
            window = handle,
            message_type = 0,
            format = 32,
            ptr1 = invokeMessageId,
            ptr2 = 0,
            ptr3 = 0,
            ptr4 = 0,
        }
    };
    XSendEvent(display, handle, false, 0, ref @event);

    XFlush(display);

    await taskCompletionSource.Task;
}

_ = Task.Run(async () =>
{
    await InvokeAsync(() =>
    {
        var mainWindowHandle = handle;

        // 再创建另一个窗口设置 Owner-Owned 关系
        var childWindowHandle = XCreateSimpleWindow(display, rootWindow, 0, 0, 300, 300, 5, white, black);

        XSelectInput(display, childWindowHandle, mask);

        // 设置父子关系
        XReparentWindow(display, childWindowHandle, mainWindowHandle, 300, 50);
        XMapWindow(display, childWindowHandle);
    });

    //while (true)
    //{
    //    await Task.Delay(TimeSpan.FromSeconds(1));

    //    await InvokeAsync(() =>
    //    {
    //        Console.WriteLine($"在主线程执行 {Thread.CurrentThread.Name}");
    //    });
    //}
});

Thread.CurrentThread.Name = "主线程";

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
    if (xNextEvent != 0)
    {
        Console.WriteLine($"xNextEvent {xNextEvent}");
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        XDrawLine(display, handle, gc, 0, 0, 100, 100);
    }
    else if (@event.type == XEventName.ClientMessage)
    {
        var clientMessageEvent = @event.ClientMessageEvent;
        if (clientMessageEvent.message_type == 0 && clientMessageEvent.ptr1 == invokeMessageId)
        {
            List<Action> tempList;
            lock (invokeList)
            {
                tempList = invokeList.ToList();
                invokeList.Clear();
            }

            foreach (var action in tempList)
            {
                action();
            }
        }
    }
    else if (@event.type == XEventName.MotionNotify)
    {
        if (@event.MotionEvent.window == handle)
        {
            Console.WriteLine($"Window1 {DateTime.Now:HH:mm:ss}");
        }
        else
        {
            Console.WriteLine($"Window2 {DateTime.Now:HH:mm:ss}");
        }
    }
}

Console.WriteLine("Hello, World!");