using System.Runtime.Loader;
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
        XIDeviceInfo? pointerDevice = default;
        for (var c = 0; c < num; c++)
        {
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

            XiSelectEvents(Display, Window, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = multiTouchEventTypes });
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
            else if (type is (int) XiEventType.XI_TouchBegin
                    or (int) XiEventType.XI_TouchUpdate
                    or (int) XiEventType.XI_TouchEnd)
            {
                Console.WriteLine($"Touch");
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
        using var background = new SKBitmap(new SKImageInfo((int) skRect.Width, (int) skRect.Height, _skBitmap.ColorType, _skBitmap.AlphaType));
        using (var backgroundCanvas = new SKCanvas(background))
        {
            backgroundCanvas.DrawBitmap(_skBitmap, skRect, new SKRect(0, 0, skRect.Width, skRect.Height));
        }

        //skCanvas.Clear(SKColors.RosyBrown);

        skPaint.Color = new SKColor(0x12, 0x56, 0x22, 0xF1);
        skCanvas.DrawRect(skRect, skPaint);

        skCanvas.DrawBitmap(background, new SKRect(0, 0, skRect.Width, skRect.Height), new SKRect(0, 0, skRect.Width, skRect.Height));
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