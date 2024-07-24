using CPF.Linux;

using Microsoft.Maui.Graphics;

using ReewheaberekaiNayweelehe;

using SkiaInkCore.Diagnostics;
using SkiaInkCore.Interactives;

using SkiaSharp;

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

var xDisplayWidth = XDisplayWidth(display, screen);
var xDisplayHeight = XDisplayHeight(display, screen);

var width = xDisplayWidth;
var height = xDisplayHeight;

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

unsafe
{
    var devices = (XIDeviceInfo*) XLib.XIQueryDevice(display,
        (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);

    XIDeviceInfo? pointerDevice = default;
    for (var c = 0; c < num; c++)
    {
        StaticDebugLogger.WriteLine($"XIDeviceInfo [{c}] {devices[c].Deviceid} {devices[c].Use}");

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
}

var gc = XCreateGC(display, handle, 0, 0);
var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
var skCanvas = new SKCanvas(skBitmap);
var xImage = CreateImage(skBitmap);

skCanvas.Clear(SKColors.Blue);
skCanvas.Flush();

var skInkCanvas = new SkInkCanvas(skCanvas, skBitmap);
skInkCanvas.RenderBoundsChanged += (sender, rect) =>
{
    var xEvent = new XEvent
    {
        ExposeEvent =
        {
            type = XEventName.Expose,
            send_event = true,
            window = handle,
            count = 1,
            display = display,
            height = (int)rect.Height,
            width = (int)rect.Width,
            x = (int)rect.X,
            y = (int)rect.Y
        }
    };
    // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
    XSendEvent(display, handle, propagate: false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
};
var input = new InkingInputManager(skInkCanvas);

bool isDown = false;

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        XPutImage(display, handle, gc, ref xImage, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x,
            @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
            (uint) @event.ExposeEvent.height);
    }
    else if (@event.type == XEventName.ButtonPress)
    {
        var x = @event.ButtonEvent.x;
        var y = @event.ButtonEvent.y;
        input.Down(new InkingModeInputArgs(0, new Point(x, y), (ulong) Environment.TickCount64));
        isDown = true;
    }
    else if (@event.type == XEventName.MotionNotify)
    {
        if (isDown)
        {
            var x = @event.MotionEvent.x;
            var y = @event.MotionEvent.y;
            input.Move(new InkingModeInputArgs(0, new Point(x, y), (ulong) Environment.TickCount64));
        }

        while (true)
        {
            XPeekEvent(display, out var nextEvent);
            if (nextEvent.type == XEventName.MotionNotify)
            {
                XNextEvent(display, out _);
            }
            else
            {
                break;
            }
        }
    }
    else if (@event.type == XEventName.ButtonRelease)
    {
        var x = @event.ButtonEvent.x;
        var y = @event.ButtonEvent.y;
        input.Up(new InkingModeInputArgs(0, new Point(x, y), (ulong) Environment.TickCount64));
        isDown = false;
    }
    else if (@event.type == XEventName.GenericEvent)
    {
        unsafe
        {
            void* data = &@event.GenericEventCookie;
            XGetEventData(display, data);
            try
            {
                var xiEvent = (XIEvent*) @event.GenericEventCookie.data;
                if (xiEvent->evtype is
                    XiEventType.XI_ButtonPress
                    or XiEventType.XI_ButtonRelease
                    or XiEventType.XI_Motion
                    or XiEventType.XI_TouchBegin
                    or XiEventType.XI_TouchUpdate
                    or XiEventType.XI_TouchEnd
                   )
                {
                    var xiDeviceEvent = (XIDeviceEvent*) xiEvent;
                    bool isMouse = false;
                    var timestamp = (ulong) xiDeviceEvent->time.ToInt64();

                    if (xiDeviceEvent->evtype is
                        XiEventType.XI_ButtonPress
                        or XiEventType.XI_ButtonRelease
                        or XiEventType.XI_Motion)
                    {
                        if ((xiDeviceEvent->flags & XiDeviceEventFlags.XIPointerEmulated) ==
                            XiDeviceEventFlags.XIPointerEmulated)
                        {
                            // 多指触摸下是模拟的，则忽略
                            //Console.WriteLine("多指触摸下是模拟的");
                            continue;
                        }

                        isMouse = true;
                    }

                    var id = xiDeviceEvent->detail;
                    if (isMouse)
                    {
                        // 由于在 XI_ButtonPress 时的 id 是 1 而 XI_Motion 是 0 导致无法画出线
                        id = 0;
                    }

                    if (xiDeviceEvent->evtype is XiEventType.XI_ButtonPress or XiEventType.XI_TouchBegin)
                    {
                        input.Down(new InkingModeInputArgs(id, new Point(xiDeviceEvent->event_x, xiDeviceEvent->event_y), timestamp));
                        isDown = true;
                    }
                    else if (xiDeviceEvent->evtype is XiEventType.XI_Motion or XiEventType.XI_TouchUpdate)
                    {
                        if (isDown)
                        {
                            input.Move(new InkingModeInputArgs(id, new Point(xiDeviceEvent->event_x, xiDeviceEvent->event_y), timestamp));
                        }
                    }
                    else if (xiDeviceEvent->evtype is XiEventType.XI_ButtonRelease or XiEventType.XI_TouchEnd)
                    {
                        input.Up(new InkingModeInputArgs(id, new Point(xiDeviceEvent->event_x, xiDeviceEvent->event_y), timestamp));
                        isDown = false;
                    }
                }
            }
            finally
            {
                XFreeEventData(display, data);
            }
        }
    }
}

static XImage CreateImage(SKBitmap skBitmap)
{
    const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
    var bitPerByte = 8;

    var bitmapWidth = skBitmap.Width;
    var bitmapHeight = skBitmap.Height;

    var img = new XImage();
    int bitsPerPixel = bytePerPixelCount * bitPerByte;
    img.width = bitmapWidth;
    img.height = bitmapHeight;
    img.format = 2; //ZPixmap;
    img.data = skBitmap.GetPixels();
    img.byte_order = 0; // LSBFirst;
    img.bitmap_unit = bitsPerPixel;
    img.bitmap_bit_order = 0; // LSBFirst;
    img.bitmap_pad = bitsPerPixel;
    img.depth = bitsPerPixel;
    img.bytes_per_line = bitmapWidth * bytePerPixelCount;
    img.bits_per_pixel = bitsPerPixel;
    XInitImage(ref img);

    return img;
}