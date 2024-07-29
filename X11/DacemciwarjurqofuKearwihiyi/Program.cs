using System.Diagnostics;
using CPF.Linux;

using SkiaSharp;

using System.Diagnostics.Tracing;

using static CPF.Linux.XLib;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

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

var gc = XCreateGC(display, handle, 0, 0);
var skBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
var skCanvas = new SKCanvas(skBitmap);
var xImage = CreateImage(skBitmap);

skCanvas.Clear(SKColors.Blue);

using var skPaint = new SKPaint();
skPaint.Color = SKColors.Black;
skPaint.StrokeWidth = 2;
skPaint.Style = SKPaintStyle.Stroke;

for (int y = 0; y < skBitmap.Height; y += 25)
{
    skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
    skCanvas.DrawLine(0, y, skBitmap.Width, y, skPaint);
}

for (int x = 0; x < skBitmap.Width; x += 25)
{
    skPaint.Color = new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF);
    skCanvas.DrawLine(x, 0, x, skBitmap.Height, skPaint);
}

skCanvas.Flush();

skCanvas.Clear(SKColors.White);
var typeface = SKFontManager.Default.MatchCharacter('林');
skPaint.TextSize = 20;
skPaint.Typeface = typeface;
skPaint.Color = SKColors.Black;
skCanvas.DrawText("中文", 100, 100, skPaint);
skCanvas.Clear(SKColors.White);

unsafe
{
    var devices = (XIDeviceInfo*) XIQueryDevice(display,
        (int) XiPredefinedDeviceId.XIAllMasterDevices, out int num);

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
        var multiTouchEventTypes = new List<XiEventType>
        {
            XiEventType.XI_TouchBegin,
            XiEventType.XI_TouchUpdate,
            XiEventType.XI_TouchEnd
        };

        XiSelectEvents(display, handle, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = multiTouchEventTypes });
    }
}

var dictionary = new Dictionary<int, TouchInfo>();
bool isSendExposeEvent = false;

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        XPutImage(display, handle, gc, ref xImage, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x, @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
            (uint) @event.ExposeEvent.height);
        isSendExposeEvent = false;
    }
    else if (@event.type == XEventName.MotionNotify)
    {
        var x = @event.MotionEvent.x;
        var y = @event.MotionEvent.y;

        skCanvas.Clear(new SKColor((uint) Random.Shared.Next()).WithAlpha(0xFF));
        skCanvas.Flush();

        SendExposeEvent(display, handle, 0, 0, width, height);
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
                    XiEventType.XI_ButtonRelease
                    or XiEventType.XI_ButtonRelease
                    or XiEventType.XI_Motion
                    or XiEventType.XI_TouchBegin
                    or XiEventType.XI_TouchUpdate
                    or XiEventType.XI_TouchEnd)
                {
                    var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                    //var name = "微软雅黑";
                    //var skTypeface = SKTypeface.FromFamilyName(name);
                    var x = xiDeviceEvent->event_x;
                    var y = xiDeviceEvent->event_y;
                    if (xiEvent->evtype == XiEventType.XI_TouchBegin)
                    {
                        dictionary[xiDeviceEvent->detail] = new TouchInfo(xiDeviceEvent->detail, x, y, false);
                    }
                    else if (xiEvent->evtype == XiEventType.XI_TouchUpdate)
                    {
                        if (dictionary.TryGetValue(xiDeviceEvent->detail, out var t))
                        {
                            dictionary[xiDeviceEvent->detail] = t with
                            {
                                X = x,
                                Y = y,
                            };
                        }
                    }
                    else if (xiEvent->evtype == XiEventType.XI_TouchEnd)
                    {
                        if (dictionary.TryGetValue(xiDeviceEvent->detail, out var t))
                        {
                            dictionary[xiDeviceEvent->detail] = t with
                            {
                                X = x,
                                Y = y,
                                IsUp = true,
                            };
                        }
                    }

                    Draw();
                }
            }
            finally
            {

            }
        }
    }
}

void Draw()
{
    skCanvas.Clear(SKColors.White);

    foreach (var value in dictionary.Values)
    {
        skPaint.IsLinearText = false;
        var text = $"""
                    Id={value.Id}
                    X={value.X} Y={value.Y}
                    """;
        if (value.IsUp)
        {
            text = "[已抬起]\r\n" + text;
        }

        skCanvas.DrawText(text, (float) value.X, (float) value.Y, skPaint);
    }

    if (isSendExposeEvent)
    {
        return;
    }

    SendExposeEvent(display, handle, 0, 0, width, height);
    isSendExposeEvent = true;
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

static void SendExposeEvent(IntPtr display, IntPtr window, int x, int y, int width, int height)
{
    var exposeEvent = new XExposeEvent
    {
        type = XEventName.Expose,
        display = display,
        window = window,
        x = x,
        y = y,
        width = width,
        height = height,
        count = 1,
    };

    var xEvent = new XEvent
    {
        ExposeEvent = exposeEvent
    };

    XSendEvent(display, window, false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
    XFlush(display);
}

record TouchInfo(int Id, double X, double Y, bool IsUp);