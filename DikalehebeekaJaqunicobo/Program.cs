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
    while (true)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        await InvokeAsync(() =>
        {
            Console.WriteLine($"在主线程执行 {Thread.CurrentThread.Name}");
        });
    }
<<<<<<< HEAD

    //if (long.TryParse(args[1], out var otherProcessGCHandle))
    //{
    //    window2GCHandle = new IntPtr(otherProcessGCHandle);
    //}
    // 不用别人传的，从窗口进行创建
    window2GCHandle = XCreateGC(display, window2Handle, 0, 0);
}

XIDeviceInfo? pointerDevice = default;
unsafe
{
    var devices = (XIDeviceInfo*) XLib.XIQueryDevice(display,
        (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);

    for (var c = 0; c < num; c++)
    {
        if (devices[c].Use == XiDeviceType.XIMasterPointer)
        {
            pointerDevice = devices[c];
            break;
        }
    }
}

if (pointerDevice != null)
{
    XiEventType[] multiTouchEventTypes =
    [
        XiEventType.XI_TouchBegin,
        XiEventType.XI_TouchUpdate,
        XiEventType.XI_TouchEnd
    ];

    XiEventType[] defaultEventTypes =
    [
        XiEventType.XI_Motion,
        XiEventType.XI_ButtonPress,
        XiEventType.XI_ButtonRelease,
        XiEventType.XI_Leave,
        XiEventType.XI_Enter,
    ];

    List<XiEventType> eventTypes = [.. multiTouchEventTypes, .. defaultEventTypes];

    XiSelectEvents(display, window1.Window, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = eventTypes });
}

=======
});
>>>>>>> 33dbc36c47d6f1e68265c0f0f389a566823425fd

Thread.CurrentThread.Name = "主线程";

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
    if (xNextEvent != 0)
    {
        Console.WriteLine($"xNextEvent {xNextEvent}");
        break;
    }

    if (args.Length == 0)
    {
        Console.WriteLine(@event.type);
    }

    if (@event.type == XEventName.Expose)
    {
        XDrawLine(display, handle, gc, 0, 0, 100, 100);
    }
<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
    else if (@event.type == XEventName.MotionNotify)
    {
        var x = @event.MotionEvent.x;
        var y = @event.MotionEvent.y;

        if (window2Handle != 0 && window2GCHandle != 0)
        {
            // 绘制是无效的
            //XDrawLine(display, window2Handle, window2GCHandle, x, y, x + 100, y);

            var xEvent = new XEvent
            {
                MotionEvent =
                {
                    type = XEventName.MotionNotify,
                    send_event = true,
                    window = window2Handle,
                    display = display,
                    x = x,
                    y = y
                }
            };
            XSendEvent(display, window2Handle, propagate: false, new IntPtr((int) (EventMask.ButtonMotionMask)),
                ref xEvent);
        }
        else
        {
            XDrawLine(display, window1.Window, window1.GC, x, y, x + 100, y);
        }

        //if (@event.MotionEvent.window == window1.Window)
        //{
        //    XDrawLine(display, window1.Window, window1.GC, x, y, x + 100, y);
        //}
        //else
        //{
        //    var xEvent = new XEvent
        //    {
        //        MotionEvent =
        //        {
        //            type = XEventName.MotionNotify,
        //            send_event = true,
        //            window = window1.Window,
        //            display = display,
        //            x = x,
        //            y = y
        //        }
        //    };
        //    XSendEvent(display, window1.Window, propagate: false, new IntPtr((int)(EventMask.ButtonMotionMask)),
        //        ref xEvent);
        //}
    }
<<<<<<< HEAD
    else if (@event.type == XEventName.GenericEvent)
    {
        unsafe
        {
            var eventCookie = @event.GenericEventCookie;
            void* data = &eventCookie;
            XGetEventData(display, data);
            try
            {
                var xiEvent = (XIEvent*) eventCookie.data;

                if (xiEvent->evtype is
                     XiEventType.XI_Motion
                    or XiEventType.XI_TouchUpdate)
                {

                    var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                    var x = (int) xiDeviceEvent->event_x;
                    var y = (int) xiDeviceEvent->event_y;

                    //Console.WriteLine($"copyXIDeviceEvent.RootWindow={xiDeviceEvent->RootWindow} rootWindow={rootWindow}"); // 两个进程是相同的

                    if (window2Handle != 0 && window2GCHandle != 0)
                    {
                        XIDeviceEvent copyXIDeviceEvent = *xiDeviceEvent;
                        copyXIDeviceEvent.EventWindow = window2Handle;

                        //Console.WriteLine($"extension={eventCookie.extension}");

                        var xEvent = new XEvent
                        {
                            GenericEventCookie =
                            {
                                type = (int) XEventName.GenericEvent,
                                send_event = true,
                                display = display,
                                cookie = eventCookie.cookie,
                                evtype = eventCookie.evtype,
                                //extension = eventCookie.extension, // 设置了会炸掉
                                serial = eventCookie.serial, // Serial number of failed request
                                data = &copyXIDeviceEvent,
                                //data = (void*) 0
                            }
                        };
                        var status = XSendEvent(display, window2Handle, propagate: false, new IntPtr(0),
                             ref xEvent);
                        Console.WriteLine($"SendEvent {status}");
                    }
                    else
                    {
                        XDrawLine(display, window1.Window, window1.GC, x, y, x + 100, y);
                    }
                }
            }
            finally
            {
                XFreeEventData(display, data);
            }
        }
=======

<<<<<<< HEAD
    var count = XEventsQueued(display, 0 /*QueuedAlready*/);
    if (count == 0)
    {
        var result = XIconifyWindow(display, window1.Window, screen);
        Console.WriteLine($"XIconifyWindow {result}");
>>>>>>> 86cbdc30df6681d1a8da8a287f2cdcc44f9e8e8f
    }
=======
    // 这是有用的
    //var count = XEventsQueued(display, 0 /*QueuedAlready*/);
    //if (count == 0)
    //{
    //    var result = XIconifyWindow(display, window1.Window, screen);
    //    Console.WriteLine($"XIconifyWindow {result}");
    //}
>>>>>>> 4824a4bc1e13ba0da4e7d4a67b93a75e12ee99a5
=======
>>>>>>> 33dbc36c47d6f1e68265c0f0f389a566823425fd
=======

    Console.WriteLine(@event.type);
>>>>>>> 442250a21eb41b8fb07dc30c14d07e629fbc452f
=======
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
    else
    {
        Console.WriteLine(@event.type);
    }
>>>>>>> c934a9562639e47095686ccf0137f58130e87835
}

Console.WriteLine("Hello, World!");