using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CPF.Linux;
using SkiaSharp;
using static CPF.Linux.XLib;
using static CPF.Linux.XShm;
using static CPF.Linux.LibC;

unsafe
{
    XInitThreads();

    var display = XOpenDisplay(IntPtr.Zero);
    var screen = XDefaultScreen(display);
    var rootWindow = XDefaultRootWindow(display);

    XMatchVisualInfo(display, screen, 32, 4, out var info);
    var visual = info.visual;

    var xDisplayWidth = XDisplayWidth(display, screen);
    var xDisplayHeight = XDisplayHeight(display, screen);

    var width = xDisplayWidth;
    var height = xDisplayHeight;

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

    var handle = XCreateWindow(display, rootWindow, 0, 0, width, height, 5,
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

    var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

    var mapLength = width * 4 * height;
    //Console.WriteLine($"Length = {mapLength}");

    var status = XShmQueryExtension(display);
    if (status == 0)
    {
        Console.WriteLine("XShmQueryExtension failed");
    }

    status = XShmQueryVersion(display, out var major, out var minor, out var pixmaps);
    Console.WriteLine($"XShmQueryVersion: {status} major={major} minor={minor} pixmaps={pixmaps}");

    const int ZPixmap = 2;
    var xShmSegmentInfo = new XShmSegmentInfo();
    var shmImage = (XImage*) XShmCreateImage(display, visual, 32, ZPixmap, IntPtr.Zero, &xShmSegmentInfo,
        (uint) width, (uint) height);

    Console.WriteLine($"XShmCreateImage = {(IntPtr) shmImage:X} xShmSegmentInfo={xShmSegmentInfo}");

    var shmgetResult = shmget(IPC_PRIVATE, mapLength, IPC_CREAT | 0777);
    Console.WriteLine($"shmgetResult={shmgetResult:X}");

    xShmSegmentInfo.shmid = shmgetResult;

    var shmaddr = shmat(shmgetResult, IntPtr.Zero, 0);
    Console.WriteLine($"shmaddr={shmaddr:X}");

    xShmSegmentInfo.shmaddr = (char*) shmaddr.ToPointer();
    shmImage->data = shmaddr;

    XShmAttach(display, &xShmSegmentInfo);
    XFlush(display);

    var gc = XCreateGC(display, handle, 0, 0);

    XFlush(display);

    // 多指触摸
    var devices = (XIDeviceInfo*) XIQueryDevice(display,
        (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);
    Console.WriteLine($"DeviceNumber={num}");
    XIDeviceInfo? pointerDevice = default;
    for (var c = 0; c < num; c++)
    {
        Console.WriteLine($"XIDeviceInfo [{c}] {devices[c].Deviceid} {devices[c].Use}");

        if (devices[c].Use == XiDeviceType.XIMasterPointer)
        {
            pointerDevice = devices[c];
            break;
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

        XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = eventTypes });
    }

    // 测试高 CPU 情况下，依然能够在服务端快速处理完成
    //for (int i = 0; i < 6; i++)
    //{
    //    Task.Run(() =>
    //    {
    //        while (true)
    //        {

    //        }
    //    });
    //}

    Task.Run(() =>
    {
        var newDisplay = XOpenDisplay(IntPtr.Zero);

        while (true)
        {
            Console.ReadLine();

            var xEvent = new XEvent
            {
                ExposeEvent =
                {
                    type = XEventName.Expose,
                    send_event = true,
                    window = handle,
                    count = 1,
                    display = newDisplay,
                    x = 0,
                    y = 0,
                    width = width,
                    height = height,
                }
            };
            // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
            XLib.XSendEvent(newDisplay, handle, propagate: false,
                new IntPtr((int) (EventMask.ExposureMask)),
                ref xEvent);

            XFlush(newDisplay);
        }

        XCloseDisplay(newDisplay);
    });

    bool isRequestRender = false;
    bool isRenderFinish = true;

    var stopwatch = new Stopwatch();

    while (true)
    {
        var xNextEvent = XNextEvent(display, out var @event);

        var count = XPending(display);
        Console.WriteLine($"卡顿度： {count}");

        if (xNextEvent != 0)
        {
            break;
        }

        if (@event.type == XEventName.Expose)
        {
            // 模拟绘制界面
            var color = Random.Shared.Next();
            color = (color | 0xFF << 24);
            for (int i = 0; i < mapLength / 4; i++)
            {
                var p = (int*) shmaddr;
                p[i] = color;
            }

            stopwatch.Restart();

            Console.WriteLine($"shmseg={xShmSegmentInfo.shmseg}");

            XShmPutImage(display, handle, gc, (XImage*) shmImage, 0, 0, 0, 0, (uint) width, (uint) height, true);

            XFlush(display);
        }
        else if (@event.type == XEventName.GenericEvent)
        {
            void* data = &@event.GenericEventCookie;
            XGetEventData(display, data);
            var xiEvent = (XIEvent*) @event.GenericEventCookie.data;
            if (xiEvent->evtype is
                XiEventType.XI_ButtonPress
                or XiEventType.XI_ButtonRelease
                or XiEventType.XI_Motion
                or XiEventType.XI_TouchBegin
                or XiEventType.XI_TouchUpdate
                or XiEventType.XI_TouchEnd)
            {
                var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                if (xiDeviceEvent->evtype == XiEventType.XI_TouchUpdate)
                {
                    var x = xiDeviceEvent->event_x;
                    var y = xiDeviceEvent->event_y;

                    using (var skCanvas = new SKCanvas(skBitmap))
                    {
                        skCanvas.Clear();
                        using var skPaint = new SKPaint();
                        skPaint.Color = SKColors.Red;
                        skPaint.Style = SKPaintStyle.Fill;

                        skCanvas.DrawRect((float) x, (float) y, 100, 100, skPaint);
                    }

                    if (isRenderFinish)
                    {
                        SendRender();
                    }
                    else
                    {
                        isRequestRender = true;
                    }
                }
            }

        }
        else if ((int) @event.type == 65 /*XShmCompletionEvent*/)
        {
            isRenderFinish = true;

            var p = &@event;
            var xShmCompletionEvent = (XShmCompletionEvent*) p;

            stopwatch.Stop();

            //Console.WriteLine($"XShmCompletionEvent: type={xShmCompletionEvent->type} serial={xShmCompletionEvent->serial} {xShmCompletionEvent->send_event} {xShmCompletionEvent->display} {xShmCompletionEvent->drawable} ShmReqCode={xShmCompletionEvent->major_code} X_ShmPutImage={xShmCompletionEvent->minor_code} shmseg={xShmCompletionEvent->shmseg} {xShmCompletionEvent->offset}");
            Console.WriteLine($"消费耗时: {stopwatch.ElapsedMilliseconds}");

            if (isRequestRender)
            {
                SendRender();
            }
        }
    }

    void SendRender()
    {
        var pixels = skBitmap.GetPixels();
        Unsafe.CopyBlockUnaligned((void*) shmaddr, (void*) pixels, (uint) mapLength);

        XShmPutImage(display, handle, gc, (XImage*) shmImage, 0, 0, 0, 0, (uint) width, (uint) height, true);
        XFlush(display);

        stopwatch.Restart();
    }
}
