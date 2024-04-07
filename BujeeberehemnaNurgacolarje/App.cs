﻿using System.Runtime.Loader;
using static CPF.Linux.XLib;
using CPF.Linux;
using System.Runtime.InteropServices;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using System.Reflection.Metadata;

namespace BujeeberehemnaNurgacolarje;

public class App
{
    public App()
    {
        XInitThreads();
        Display = XOpenDisplay(IntPtr.Zero);
        XError.Init();

        Info = new X11Info(Display, DeferredDisplay);
        Console.WriteLine("XInputVersion=" + Info.XInputVersion);
        var screen = XDefaultScreen(Display);
        Console.WriteLine($"Screen = {screen}");
        Screen = screen;
        var white = XWhitePixel(Display, screen);
        var black = XBlackPixel(Display, screen);

        var rootWindow = XDefaultRootWindow(Display);

        XMatchVisualInfo(Display, screen, 32, 4, out var info);
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
            colormap = XCreateColormap(Display, rootWindow, visual, 0),
            border_pixel = 0,
            background_pixel = 0,
        };

        var handle = XCreateWindow(Display, rootWindow, 0, 0, XDisplayWidth(Display, screen), XDisplayHeight(Display, screen), 5,
            32,
            (int) CreateWindowArgs.InputOutput,
            visual,
            (nuint) valueMask, ref xSetWindowAttributes);

        Window = handle;

        //Window = XCreateSimpleWindow(Display, rootWindow, 0, 0, 500, 300, 5, white, black);

        Console.WriteLine($"Window={Window}");

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
        XSelectInput(Display, Window, mask);

        XMapWindow(Display, Window);
        XFlush(Info.Display);

        GC = XCreateGC(Display, Window, 0, 0);
        XSetForeground(Display, GC, white);

        Console.WriteLine($"App");

        var size = 600;
        var skBitmap = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skBitmap = skBitmap;

        _skSurface = SKSurface.Create(new SKImageInfo(size, size, SKImageInfo.PlatformColorType, SKAlphaType.Premul));

        var skCanvas = _skSurface.Canvas;
        skCanvas.Clear(SKColors.Transparent);
        skCanvas.Flush();
        _skCanvas = skCanvas;

        skCanvas.DrawBitmap(_skBitmap, 0, 0);

        using var skPaint = new SKPaint()
        {
            Color = SKColors.Red,
            StrokeWidth = 5,
            IsAntialias = false,
        };
        skCanvas.DrawLine(0, 0, size, size, skPaint);
        skCanvas.DrawLine(0, size, size, 0, skPaint);

        skPaint.Color = new SKColor((uint) Random.Shared.Next());


        XImage img = CreateImage();
        _image = img;
    }

    private XImage _image;

    private const int MaxStylusCount = 100;
    private readonly FixedQueue<StylusPoint> _stylusPoints = new FixedQueue<StylusPoint>(MaxStylusCount);
    private readonly StylusPoint[] _cache = new StylusPoint[MaxStylusCount + 1];

    public unsafe void Run(nint ownerWindowIntPtr)
    {
        XSetInputFocus(Display, Window, 0, IntPtr.Zero);
        // bing 如何设置X11里面两个窗口之间的层级关系
        // bing 如何编写代码设置X11里面两个窗口之间的层级关系，比如有 a 和 b 两个窗口，如何设置 a 窗口一定在 b 窗口上方？
        // 我们使用XSetTransientForHint函数将窗口a设置为窗口b的子窗口。这将确保窗口a始终在窗口b的上方
        XSetTransientForHint(Display, ownerWindowIntPtr, Window);

        var devices = (XIDeviceInfo*) XIQueryDevice(Display,
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

        var valuators = new List<XIValuatorClassInfo>();
        var scrollers = new List<XIScrollClassInfo>();

        if (pointerDevice != null)
        {
            var multiTouchEventTypes = new List<XiEventType>
            {
                XiEventType.XI_TouchBegin,
                XiEventType.XI_TouchUpdate,
                XiEventType.XI_TouchEnd
            };

            XiSelectEvents(Display, Window, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = multiTouchEventTypes });

            Console.WriteLine($"pointerDevice.Value.NumClasses={pointerDevice.Value.NumClasses}");

            for (int i = 0; i < pointerDevice.Value.NumClasses; i++)
            {
                var xiAnyClassInfo = pointerDevice.Value.Classes[i];
                if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
                {
                    valuators.Add(*((XIValuatorClassInfo**) xiAnyClassInfo)[i]);
                }
                else if (xiAnyClassInfo->Type == XiDeviceClass.XIScrollClass)
                {
                    scrollers.Add(*((XIScrollClassInfo**) xiAnyClassInfo)[i]);
                }
            }

            foreach (var xiValuatorClassInfo in valuators)
            {
                var label = xiValuatorClassInfo.Label; //Marshal.PtrToStringAnsi(xiValuatorClassInfo.Label);
                Console.WriteLine($"[Valuator] {label} Type={xiValuatorClassInfo.Type} Sourceid={xiValuatorClassInfo.Sourceid} Number={xiValuatorClassInfo.Number} Min={xiValuatorClassInfo.Min} Max={xiValuatorClassInfo.Max} Value={xiValuatorClassInfo.Value} Resolution={xiValuatorClassInfo.Resolution} Mode={xiValuatorClassInfo.Mode}");
            }
        }

        while (true)
        {
            XSync(Display, false);

            var xNextEvent = XNextEvent(Display, out var @event);
            //Console.WriteLine($"NextEvent={xNextEvent} {@event}");
            int type = (int) @event.type;

            if (@event.type == XEventName.Expose)
            {
                //// 曝光时，可以收到需要重新绘制的范围
                //Console.WriteLine(
                //    $"Expose X={@event.ExposeEvent.x} Y={@event.ExposeEvent.y} W={@event.ExposeEvent.width} H={@event.ExposeEvent.height} CurrentWindow={@event.ExposeEvent.window == Window}");

                XPutImage(Display, Window, GC, ref _image, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x, @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
                    (uint) @event.ExposeEvent.height);

                Redraw();
            }
            else if (@event.type == XEventName.ButtonPress)
            {
                _lastPoint = (@event.ButtonEvent.x, @event.ButtonEvent.y);
                _isDown = true;
            }
            else if (@event.type == XEventName.MotionNotify)
            {
                if (_isDown)
                {
                    var x = @event.MotionEvent.x;
                    var y = @event.MotionEvent.y;

                    if (x < (XDisplayWidth(Display, Screen) / 2))
                    {
                        var currentStylusPoint = new StylusPoint(x, y);

                        if (DrawStroke(currentStylusPoint, out var rect))
                        {
                            var xEvent = new XEvent
                            {
                                ExposeEvent =
                                {
                                    type = XEventName.Expose,
                                    send_event = true,
                                    window = Window,
                                    count = 1,
                                    display = Display,
                                    height = (int)rect.Height,
                                    width = (int)rect.Width,
                                    x = (int)rect.X,
                                    y = (int)rect.Y
                                }
                            };
                            // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
                            XSendEvent(Display, Window, propagate: false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
                        }
                    }
                    else
                    {
                        XDrawLine(Display, Window, GC, _lastPoint.X, _lastPoint.Y, x, y);
                    }

                    _lastPoint = (x, y);
                }
            }
            else if (@event.type == XEventName.ButtonRelease)
            {
                _isDown = false;
                _stylusPoints.Clear();
            }
            // 对于 X12 触摸，这里的 type 是 GenericEvent 类型，需要通过 GenericEventCookie 的 evtype 才能判断
            //else if (type is (int) XiEventType.XI_TouchBegin
            //        or (int) XiEventType.XI_TouchUpdate
            //        or (int) XiEventType.XI_TouchEnd)
            //{
            //    Console.WriteLine($"Touch {(XiEventType) type} {@event.MotionEvent.x} {@event.MotionEvent.y}");
            //}
            else if (@event.type == XEventName.GenericEvent)
            {
                void* data = &@event.GenericEventCookie;
                /*
                 bing:
                `XGetEventData` 是一个用于 **X Window System** 的函数，其主要目的是通过 **cookie** 来检索和释放附加的事件数据。让我们来详细了解一下：

                   - **函数名称**：`XGetEventData`
                   - **功能**：检索通过 **cookie** 存储的附加事件数据。
                   - **参数**：
                       - `display`：指定与 X 服务器的连接。
                       - `cookie`：指定要释放或检索数据的 **cookie**。
                   - **结构体**：`XGenericEventCookie`
                       - `type`：事件类型。
                       - `serial`：事件序列号。
                       - `send_event`：是否为发送事件。
                       - `display`：指向 X 服务器的指针。
                       - `extension`：扩展信息。
                       - `evtype`：事件类型。
                       - `cookie`：唯一标识此事件的 **cookie**。
                       - `data`：事件数据的指针，在调用 `XGetEventData` 之前未定义。
                   - **描述**：某些扩展的 `XGenericEvents` 需要额外的内存来存储信息。对于这些事件，库会返回一个具有唯一标识此事件的 **cookie** 的 `XGenericEventCookie`。直到调用 `XGetEventData`，`XGenericEventCookie` 的数据指针是未定义的。`XGetEventData` 函数检索给定 **cookie** 的附加数据。不需要与服务器进行往返通信。如果 **cookie** 无效或事件不是由 **cookie** 处理程序处理的事件，则返回 `False`。如果 `XGetEventData` 返回 `True`，则 **cookie** 的数据指针指向包含事件信息的内存。客户端必须调用 `XFreeEventData` 来释放此内存。对于同一事件 **cookie** 的多次调用，`XGetEventData` 返回 `False`。`XFreeEventData` 函数释放与 **cookie** 关联的数据。客户端必须对使用 `XGetEventData` 获得的每个 **cookie** 调用 `XFreeEventData`。
                   - **注意事项**：
                       - 如果 **cookie** 已通过 `XNextEvent` 返回给客户端，但其数据尚未通过 `XGetEventData` 检索，则该 **cookie** 被定义为未声明。后续对 `XNextEvent` 的调用可能会释放与未声明 **cookie** 关联的内存。
                       - 多线程的 X 客户端必须确保在下一次调用 `XNextEvent` 之前调用 `XGetEventData`。

                   更多信息，请参阅 [XGetEventData 文档](https://www.x.org/releases/X11R7.6/doc/man/man3/XGetEventData.3.xhtml)。¹²

                   源: 与必应的对话， 2024/4/7
                   (1) XGetEventData - X Window System. https://www.x.org/releases/X11R7.6/doc/man/man3/XGetEventData.3.xhtml.
                   (2) XGetEventData(3) — libX11-devel. https://man.docs.euro-linux.com/EL%209/libX11-devel/XGetEventData.3.en.html.
                   (3) X11R7.7 Manual Pages: Section 3: Library Functions - X Window System. https://www.x.org/releases/X11R7.7/doc/man/man3/.
                 */
                XGetEventData(Display, data);
                try
                {
                    var xiEvent = (XIEvent*) @event.GenericEventCookie.data;
                    if (xiEvent->evtype == XiEventType.XI_DeviceChanged)
                    {
                    }

                    if (xiEvent->evtype is
                        XiEventType.XI_ButtonRelease
                        or XiEventType.XI_ButtonRelease
                        or XiEventType.XI_Motion
                        or XiEventType.XI_TouchBegin
                        or XiEventType.XI_TouchUpdate
                        or XiEventType.XI_TouchEnd)
                    {
                        var xiDeviceEvent = (XIDeviceEvent*) xiEvent;

                        var timestamp = (ulong) xiDeviceEvent->time.ToInt64();
                        var state = (XModifierMask) xiDeviceEvent->mods.Effective;

                        Console.WriteLine($"[{xiEvent->evtype}][{xiDeviceEvent->deviceid}][{xiDeviceEvent->sourceid}] detail={xiDeviceEvent->detail} timestamp={timestamp} {state} X={xiDeviceEvent->event_x} Y={xiDeviceEvent->event_y}");
                    }
                }
                finally
                {
                    /*
                     bing:
                       如果不调用 `XFreeEventData`，会导致一些潜在问题和资源泄漏。让我详细解释一下：

                       - **资源泄漏**：`XGetEventData` 函数会分配内存来存储事件数据。如果不调用 `XFreeEventData` 来释放这些内存，会导致内存泄漏。这可能会在长时间运行的应用程序中累积，最终导致内存耗尽或应用程序崩溃。

                       - **未定义行为**：如果不调用 `XFreeEventData`，则 `XGenericEventCookie` 的数据指针将保持未定义状态。这意味着您无法访问事件数据，从而可能导致应用程序中的错误或不一致性。

                       - **性能问题**：如果不释放事件数据，系统可能会在内部维护大量未释放的内存块，从而影响性能。

                       因此，为了避免这些问题，务必在使用 `XGetEventData` 获取事件数据后调用 `XFreeEventData` 来释放内存。这是良好的编程实践，有助于确保应用程序的稳定性和性能。
                     */
                    XFreeEventData(Display, data);
                }
            }

            if (xNextEvent != 0)
            {
                break;
            }
        }
    }

    private int DropPointCount { set; get; }

    private bool CanDropLastPoint(Span<StylusPoint> pointList, StylusPoint currentStylusPoint)
    {
        if (pointList.Length < 2)
        {
            return false;
        }

        var lastPoint = pointList[^1];

        if (Math.Pow(lastPoint.Point.X - currentStylusPoint.Point.X, 2) + Math.Pow(lastPoint.Point.Y - currentStylusPoint.Point.Y, 2) < 100)
        {
            return true;
        }

        return false;
    }

    private bool DrawStroke(StylusPoint currentStylusPoint, out Rect drawRect)
    {
        drawRect = Rect.Zero;
        if (_stylusPoints.Count == 0)
        {
            _stylusPoints.Enqueue(currentStylusPoint);

            return false;
        }

        _stylusPoints.CopyTo(_cache, 0);
        if (CanDropLastPoint(_cache.AsSpan(0, _stylusPoints.Count), currentStylusPoint) && DropPointCount < 3)
        {
            // 丢点是为了让 SimpleInkRender 可以绘制更加平滑的折线。但是不能丢太多的点，否则将导致看起来断线
            DropPointCount++;
            return false;
        }

        DropPointCount = 0;

        var lastPoint = _cache[_stylusPoints.Count - 1];
        if (currentStylusPoint == lastPoint)
        {
            return false;
        }

        _cache[_stylusPoints.Count] = currentStylusPoint;
        _stylusPoints.Enqueue(currentStylusPoint);

        Console.WriteLine($"Count={_stylusPoints.Count}");

        for (int i = 0; i < 10; i++)
        {
            if (_stylusPoints.Count - i - 1 < 0)
            {
                break;
            }

            _cache[_stylusPoints.Count - i - 1] = _cache[_stylusPoints.Count - i - 1] with
            {
                Pressure = Math.Max(Math.Min(0.1f * i, 0.5f), 0.01f)
                //Pressure = 0.3f,
            };
        }

        var pointList = _cache.AsSpan(0, _stylusPoints.Count);

        Point[] outlinePointList = SimpleInkRender.GetOutlinePointList(pointList, 20);
        _outlinePointList = outlinePointList;

        using var skPath = new SKPath();
        skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        //skPath.Close();

        var skPathBounds = skPath.Bounds;

        var additionSize = 10;
        drawRect = new Rect(skPathBounds.Left - additionSize, skPathBounds.Top - additionSize, skPathBounds.Width + additionSize * 2, skPathBounds.Height + additionSize * 2);

        var skCanvas = _skCanvas;
        //skCanvas.Clear(SKColors.Transparent);
        //skCanvas.Translate(-minX,-minY);
        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 0.1f;
        skPaint.Color = Color;
        skPaint.IsAntialias = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Fill;

        var skRect = new SKRect((float) drawRect.Left, (float) drawRect.Top, (float) drawRect.Right, (float) drawRect.Bottom);

        // 经过测试，似乎只有纯色画在下面才能没有锯齿，否则都会存在锯齿

        // 以下代码经过测试，没有真的做拷贝，依然还是随着变更而变更
        var background = new SKBitmap(new SKImageInfo((int) skRect.Width, (int) skRect.Height, _skBitmap.ColorType, _skBitmap.AlphaType));
        using (var backgroundCanvas = new SKCanvas(background))
        {
            backgroundCanvas.DrawBitmap(_skBitmap, skRect, new SKRect(0, 0, skRect.Width, skRect.Height));
            backgroundCanvas.Flush();
        }

        //skCanvas.Clear(SKColors.RosyBrown);

        //skPaint.Color = new SKColor(0x12, 0x56, 0x22, 0xF1);
        //skCanvas.DrawRect(skRect, skPaint);

        //skCanvas.DrawBitmap(background, new SKRect(0, 0, skRect.Width, skRect.Height), new SKRect(0, 0, skRect.Width, skRect.Height));
        //using var skImage = SKImage.FromBitmap(background);
        ////// 为何 Skia 在 DrawBitmap 之后进行 DrawPath 出现锯齿，即使配置了 IsAntialias 属性
        //skCanvas.DrawImage(skImage, new SKRect(0, 0, skRect.Width, skRect.Height), skRect);

        //// 只有纯色才能无锯齿


        skPaint.Color = Color;
        skCanvas.DrawPath(skPath, skPaint);
        skCanvas.Flush();

        //skPaint.Color = SKColors.GhostWhite;
        //skPaint.Style = SKPaintStyle.Stroke;
        //skPaint.StrokeWidth = 1f;
        //skCanvas.DrawPath(skPath, skPaint);

        //skPaint.Style = SKPaintStyle.Fill;
        //skPaint.Color = SKColors.White;
        //foreach (var stylusPoint in pointList)
        //{
        //    skCanvas.DrawCircle((float) stylusPoint.Point.X, (float) stylusPoint.Point.Y, 1, skPaint);
        //}

        //skPaint.Style = SKPaintStyle.Fill;
        //skPaint.Color = SKColors.Coral;
        //foreach (var point in outlinePointList)
        //{
        //    skCanvas.DrawCircle((float) point.X, (float) point.Y, 2, skPaint);

        //}
        drawRect = new Rect(0, 0, 600, 600);

        return true;
    }

    private Point[]? _outlinePointList;

    public SKColor Color { get; set; } = SKColors.Red;

    private (int X, int Y) _lastPoint;
    private bool _isDown;
    private readonly SKBitmap _skBitmap;
    private readonly SKCanvas _skCanvas;
    private readonly SKSurface _skSurface;

    private void Redraw()
    {
        //var img = _image;

        //XPutImage(Display, Window, GC, ref img, 0, 0, Random.Shared.Next(100), Random.Shared.Next(100), (uint)img.width,
        //    (uint)img.height);
    }

    private unsafe XImage CreateImage()
    {
        //var bitmapWidth = 50;
        //var bitmapHeight = 50;

        const int bytePerPixelCount = 4; // RGBA 一共4个 byte 长度
        var bitPerByte = 8;

        //var bitmapData = new byte[bitmapWidth * bitmapHeight * bytePerPixelCount];

        //fixed (byte* p = bitmapData)
        //{
        //    int* pInt = (int*) p;
        //    var color = Random.Shared.Next();
        //    for (var i = 0; i < bitmapData.Length / (sizeof(int) / sizeof(byte)); i++)
        //    {
        //        *(pInt + i) = color;
        //    }
        //}
        //GCHandle pinnedArray = GCHandle.Alloc(bitmapData, GCHandleType.Pinned);

        var bitmapWidth = _skBitmap.Width;
        var bitmapHeight = _skBitmap.Height;

        var img = new XImage();
        int bitsPerPixel = bytePerPixelCount * bitPerByte;
        img.width = bitmapWidth;
        img.height = bitmapHeight;
        img.format = 2; //ZPixmap;
        //img.data = pinnedArray.AddrOfPinnedObject();
        //img.data = _skBitmap.GetPixels();
        img.data = _skSurface.PeekPixels().GetPixels();
        img.byte_order = 0; // LSBFirst;
        img.bitmap_unit = bitsPerPixel;
        img.bitmap_bit_order = 0; // LSBFirst;
        img.bitmap_pad = bitsPerPixel;
        img.depth = bitsPerPixel;
        img.bytes_per_line = bitmapWidth * bytePerPixelCount;
        img.bits_per_pixel = bitsPerPixel;
        XInitImage(ref img);

        // 除非 XImage 不再使用了，否则此时释放，将会导致 GC 之后 data 指针对应的内存不是可用的
        // 调用 XPutImage 将访问不可用内存，导致段错误，闪退
        //pinnedArray.Free();

        return img;
    }

    private IntPtr GC { get; }

    public IntPtr DeferredDisplay { get; set; }
    public IntPtr Display { get; set; }

    //public XI2Manager XI2;
    public X11Info Info { get; private set; }
    public IntPtr Window { get; set; }
    public int Screen { get; set; }

    public void Clear()
    {
        _skCanvas.Clear(SKColors.Transparent);

        // 立刻推送，否则将不会立刻更新，只有鼠标移动到 X11 窗口上才能看到更新界面
        XPutImage(Display, Window, GC, ref _image, 0, 0, 0, 0, (uint) _skBitmap.Width,
            (uint) _skBitmap.Height);

        var xEvent = new XEvent
        {
            ExposeEvent =
            {
                type = XEventName.Expose,
                send_event = true,
                window = Window,
                count = 1,
                display = Display,
                height = (int)_skBitmap.Height,
                width = (int)_skBitmap.Width,
                x = (int)0,
                y = (int)0
            }
        };
        // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
        XSendEvent(Display, Window, propagate: false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
    }

    public void SwitchDebugMode(bool enterDebugMode)
    {
        if (_outlinePointList is null)
        {
            return;
        }

        var outlinePointList = _outlinePointList;
        using var skPath = new SKPath();
        skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        //skPath.Close();

        var skPathBounds = skPath.Bounds;

        var additionSize = 10;
        var drawRect = new Rect(skPathBounds.Left - additionSize, skPathBounds.Top - additionSize, skPathBounds.Width + additionSize * 2, skPathBounds.Height + additionSize * 2);

        if (enterDebugMode)
        {
            var skCanvas = _skCanvas;
            //skCanvas.Clear(SKColors.Black);
            //skCanvas.Translate(-minX,-minY);
            using var skPaint = new SKPaint();
            skPaint.StrokeWidth = 0.1f;
            skPaint.Color = Color;
            skPaint.IsAntialias = true;
            skPaint.Style = SKPaintStyle.Fill;
            skCanvas.DrawPath(skPath, skPaint);

            skPaint.Color = SKColors.GhostWhite;
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.StrokeWidth = 1f;
            skCanvas.DrawPath(skPath, skPaint);

            //skPaint.Style = SKPaintStyle.Fill;
            //skPaint.Color = SKColors.White;
            //foreach (var stylusPoint in pointList)
            //{
            //    skCanvas.DrawCircle((float) stylusPoint.Point.X, (float) stylusPoint.Point.Y, 1, skPaint);
            //}

            skPaint.Style = SKPaintStyle.Fill;
            skPaint.Color = SKColors.Coral;
            foreach (var point in outlinePointList)
            {
                skCanvas.DrawCircle((float) point.X, (float) point.Y, 2, skPaint);
            }
        }
        else
        {
            var skCanvas = _skCanvas;
            //skCanvas.Clear(SKColors.Transparent);
            var skRect = new SKRect((float) drawRect.Left, (float) drawRect.Top, (float) drawRect.Right, (float) drawRect.Bottom);

            using var skPaint = new SKPaint();
            skPaint.StrokeWidth = 0.1f;

            skPaint.IsAntialias = true;
            skPaint.Style = SKPaintStyle.Fill;

            skPaint.Color = SKColors.Black;
            skCanvas.DrawRect(skRect, skPaint);

            skPaint.Color = Color;
            skCanvas.DrawPath(skPath, skPaint);
        }

        var xEvent = new XEvent
        {
            ExposeEvent =
            {
                type = XEventName.Expose,
                send_event = true,
                window = Window,
                count = 1,
                display = Display,
                height = (int)drawRect.Height,
                width = (int)drawRect.Width,
                x = (int)drawRect.X,
                y = (int)drawRect.Y
            }
        };
        // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
        XSendEvent(Display, Window, propagate: false, new IntPtr((int) (EventMask.ExposureMask)), ref xEvent);
    }
}