﻿// See https://aka.ms/new-console-template for more information

using CPF.Linux;

using System;
using System.Diagnostics;
using System.Runtime;

using static CPF.Linux.XFixes;
using static CPF.Linux.XLib;
using static CPF.Linux.ShapeConst;
using System.Runtime.InteropServices;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);

int eventBase, errorBase;
if (XCompositeQueryExtension(display, out eventBase, out errorBase) == 0)
{
    Console.WriteLine("Error: Composite extension is not supported");
    XCloseDisplay(display);
    return;
}
else
{
    //Console.WriteLine("XCompositeQueryExtension");
}

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
    background_pixel = new IntPtr(0x65565656),
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

    // 在 Avalonia 里面，是通过循环读取的方式，通过 XPending 判断是否有消息
    // 如果没有消息就进入自旋判断是否有业务消息和判断是否有 XPending 消息
    // 核心使用 epoll_wait 进行等待
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

var mainWindowHandle = handle;
var childWindowHandle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);

XSelectInput(display, childWindowHandle, mask);

//var y = childWindowHandle;
//XCompositeRedirectSubwindows(display, overlayWindow, 1/*CompositeRedirectAutomatic*/);

//var region = XFixesCreateRegion(display, 0, 0);
//XFixesSetWindowShapeRegion(display, childWindowHandle, ShapeInput, 0, 0, region);

//https://www.x.org/releases/X11R7.6/doc/man/man3/XShape.3.xhtml

var region = XCreateRegion();
XShapeCombineRegion(display, childWindowHandle, ShapeInput, 0, 0, region, ShapeSet);

XSetTransientForHint(display, childWindowHandle, mainWindowHandle);

XMapWindow(display, childWindowHandle);

_ = Task.Run(async () =>
{
    while (true)
    {
        //await Task.Delay(TimeSpan.FromSeconds(1));
        long sumTick = 0;

        var count = 0;
        for (int i = 0; i < 2000; i++)
        {
            count++;
            var currentCount = count;

            var n = i;
            if (n == 1000)
            {
                i = 0;
                //await Task.Delay(TimeSpan.FromSeconds(1));
            }

            var stopwatch = Stopwatch.StartNew();
            await InvokeAsync(() =>
            {
                stopwatch.Stop();
                // 0.1 - 0.5 ms 毫秒性能差
                sumTick += stopwatch.ElapsedTicks;

                if (n == 1000)
                {
                    var tick = sumTick / 1000.0;

                    Console.WriteLine($"[{currentCount}] 跨线程调度性能 : {tick / Stopwatch.Frequency * 1000:0.00}ms {tick:0.00}Tick");

                    sumTick = 0;
                }

                //XMoveWindow(display, childWindowHandle, Random.Shared.Next(200), Random.Shared.Next(100));
            });
        }
    }
});

Thread.CurrentThread.Name = "主线程";

//unsafe
//{
//    var devices = (XIDeviceInfo*) XIQueryDevice(display,
//        (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);

//    XIDeviceInfo? pointerDevice = default;
//    for (var c = 0; c < num; c++)
//    {
//        if (devices[c].Use == XiDeviceType.XIMasterPointer)
//        {
//            pointerDevice = devices[c];
//            break;
//        }
//    }

//    if (pointerDevice != null)
//    {
//        XiEventType[] multiTouchEventTypes =
//        [
//            XiEventType.XI_TouchBegin,
//            XiEventType.XI_TouchUpdate,
//            XiEventType.XI_TouchEnd
//        ];

//        XiEventType[] defaultEventTypes =
//        [
//            XiEventType.XI_Motion,
//            XiEventType.XI_ButtonPress,
//            XiEventType.XI_ButtonRelease,
//            XiEventType.XI_Leave,
//            XiEventType.XI_Enter,
//        ];

//        List<XiEventType> eventTypes = [.. multiTouchEventTypes, .. defaultEventTypes];

//        XiSelectEvents(display, mainWindowHandle,
//            new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = eventTypes });

//        XiSelectEvents(display, childWindowHandle,
//            new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = eventTypes });
//    }
//}
List<Action> tempList = new List<Action>();

while (true)
{
    var count = XEventsQueued(display, 0 /*QueuedAlready*/);
    if (count > 1)
    {
        // 这个 count 越大证明越慢
        Console.WriteLine($"卡顿度 XEventsQueued={count}");
    }

    var xNextEvent = XNextEvent(display, out var @event);
    if (xNextEvent != 0)
    {
        Console.WriteLine($"xNextEvent {xNextEvent}");
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        if (@event.ExposeEvent.window == handle)
        {
            XDrawLine(display, handle, gc, 2, 2, xDisplayWidth - 2, xDisplayHeight - 2);
            XDrawLine(display, handle, gc, 2, xDisplayHeight - 2, xDisplayWidth - 2, 2);
        }
        else if (childWindowHandle != 0 && @event.ExposeEvent.window == childWindowHandle)
        {
            XDrawLine(display, childWindowHandle, gc, 1, 1, xDisplayWidth - 2, 1);
            XDrawLine(display, childWindowHandle, gc, 1, xDisplayHeight - 2, xDisplayWidth - 2, xDisplayHeight - 2);
            XDrawLine(display, childWindowHandle, gc, 1, 1, 1, xDisplayHeight - 2);
            XDrawLine(display, childWindowHandle, gc, xDisplayWidth - 2, xDisplayHeight - 2, xDisplayWidth - 2,
                xDisplayHeight - 2);
        }
    }
    else if (@event.type == XEventName.ClientMessage)
    {
        var clientMessageEvent = @event.ClientMessageEvent;
        if (clientMessageEvent.message_type == 0 && clientMessageEvent.ptr1 == invokeMessageId)
        {
            lock (invokeList)
            {
                tempList.AddRange(invokeList);
                invokeList.Clear();
            }

            foreach (var action in tempList)
            {
                action();
            }
            tempList.Clear();
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
    //else if (@event.type == XEventName.GenericEvent)
    //{
    //    unsafe
    //    {
    //        void* data = &@event.GenericEventCookie;
    //        XGetEventData(display, data);
    //        bool isFree = false;

    //        try
    //        {
    //            var xiEvent = (XIEvent*) @event.GenericEventCookie.data;

    //            if (xiEvent->evtype is
    //                XiEventType.XI_ButtonPress
    //                or XiEventType.XI_ButtonRelease
    //                or XiEventType.XI_Motion
    //                or XiEventType.XI_TouchBegin
    //                or XiEventType.XI_TouchUpdate
    //                or XiEventType.XI_TouchEnd)
    //            {
    //                var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

    //                if (xiDeviceEvent->EventWindow == mainWindowHandle)
    //                {
    //                    Console.WriteLine($"Window1 {DateTime.Now:HH:mm:ss}");

    //                    var xRectangle = new XRectangle()
    //                    {
    //                        x = 0,
    //                        y = 0, 
    //                        width = 1000,
    //                        height = 1000,
    //                    };
    //                    XRectangle* p = &xRectangle;
    //                    XUnionRectWithRegion(p, region, region);
    //                    XShapeCombineRegion(display, childWindowHandle, ShapeInput, 0, 0, region, ShapeSet);
    //                }
    //                else
    //                {
    //                    Console.WriteLine($"Window2 {DateTime.Now:HH:mm:ss}");

    //                    //isFree = true;
    //                    //XFreeEventData(display, data);

    //                    //// 尝试转发 但是失败
    //                    //// X Error of failed request: BadValue (integer parameter out of range for operation)
    //                    //// Major opcode of failed request: 25 ( x_sendEvent)
    //                    //// Value in failed request: 0x0
    //                    //// serial number of failed request: 28
    //                    //// current serial number in output stream: 30
    //                    //XSendEvent(display, mainWindowHandle, false, 0, ref @event);
    //                }
    //            }
    //        }
    //        finally
    //        {
    //            if (!isFree)
    //            {
    //                XFreeEventData(display, data);
    //            }
    //        }
    //    }
    //}
}

Console.WriteLine("Hello, World!");