using System.Runtime.Loader;
using static CPF.Linux.XLib;
using CPF.Linux;
using System.Runtime.InteropServices;
using Microsoft.Maui.Graphics;
using SkiaSharp;
using System.Reflection.Metadata;
using System.Reflection.Emit;
using ReewheaberekaiNayweelehe;

namespace BujeeberehemnaNurgacolarje;

public class X11App
{
    public X11App()
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
        RootWindow = rootWindow;

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

        var xDisplayWidth = XDisplayWidth(Display, screen);
        var xDisplayHeight = XDisplayHeight(Display, screen);
        var handle = XCreateWindow(Display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
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

        var skBitmap = new SKBitmap(xDisplayWidth, xDisplayHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skBitmap = skBitmap;

        //_skSurface = SKSurface.Create(new SKImageInfo(size, size, SKImageInfo.PlatformColorType, SKAlphaType.Premul));

        //var skCanvas = _skSurface.Canvas;
        var skCanvas = new SKCanvas(skBitmap);
        skCanvas.Clear();
        skCanvas.Flush();
        _skCanvas = skCanvas;

        //skCanvas.DrawBitmap(_skBitmap, 0, 0);

        using var skPaint = new SKPaint()
        {
            Color = SKColors.Red,
            StrokeWidth = 5,
            IsAntialias = true,
        };
        skCanvas.DrawLine(0, 0, xDisplayWidth, xDisplayHeight, skPaint);
        skCanvas.DrawLine(0, xDisplayHeight, xDisplayWidth, 0, skPaint);

        skPaint.Color = SKColors.White.WithAlpha(0x6F);
        skCanvas.DrawRect(0, 0, skBitmap.Width, skBitmap.Height, skPaint);

        XImage image = CreateImage();
        _image = image;

        var skInkCanvas = new SkInkCanvas()
        {
            ApplicationDrawingSkBitmap = _skBitmap,
        };
        _skInkCanvas = skInkCanvas;
    }

    #region 窗口管理

    private IntPtr RootWindow { get; }

    public void ShowTaskbarIcon(bool value)
    {
        //ChangeWMAtoms(!value, _x11.Atoms._NET_WM_STATE_SKIP_TASKBAR);
    }

    private void ChangeWMAtoms(bool enable, params IntPtr[] atoms)
    {
        if (atoms.Length != 1 && atoms.Length != 2)
        {
            throw new ArgumentException();
        }

        //if (!_mapped)
        //{
        //    XGetWindowProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, IntPtr.Zero, new IntPtr(256),
        //        false, (IntPtr) Atom.XA_ATOM, out _, out _, out var nitems, out _,
        //        out var prop);
        //    var ptr = (IntPtr*) prop.ToPointer();
        //    var newAtoms = new HashSet<IntPtr>();
        //    for (var c = 0; c < nitems.ToInt64(); c++)
        //        newAtoms.Add(*ptr);
        //    XFree(prop);
        //    foreach (var atom in atoms)
        //        if (enable)
        //            newAtoms.Add(atom);
        //        else
        //            newAtoms.Remove(atom);

        //    XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, (IntPtr) Atom.XA_ATOM, 32,
        //        PropertyMode.Replace, newAtoms.ToArray(), newAtoms.Count);
        //}
        var wmState = XInternAtom(Display, "_NET_WM_STATE", true);

        SendNetWMMessage(wmState,
            (IntPtr) (enable ? 1 : 0),
            atoms[0],
            atoms.Length > 1 ? atoms[1] : IntPtr.Zero,
            atoms.Length > 2 ? atoms[2] : IntPtr.Zero,
            atoms.Length > 3 ? atoms[3] : IntPtr.Zero
         );
    }

    private void SendNetWMMessage(IntPtr message_type, IntPtr l0,
        IntPtr? l1 = null, IntPtr? l2 = null, IntPtr? l3 = null, IntPtr? l4 = null)
    {
        var xev = new XEvent
        {
            ClientMessageEvent =
            {
                type = XEventName.ClientMessage,
                send_event = true,
                window = Window,
                message_type = message_type,
                format = 32,
                ptr1 = l0,
                ptr2 = l1 ?? IntPtr.Zero,
                ptr3 = l2 ?? IntPtr.Zero,
                ptr4 = l3 ?? IntPtr.Zero
            }
        };
        xev.ClientMessageEvent.ptr4 = l4 ?? IntPtr.Zero;
        XSendEvent(Display, RootWindow, false,
            new IntPtr((int) (EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
    }

    #endregion

    private XImage _image;

    private const int MaxStylusCount = 100;
    private readonly FixedQueue<StylusPoint> _stylusPoints = new FixedQueue<StylusPoint>(MaxStylusCount);
    private readonly StylusPoint[] _cache = new StylusPoint[MaxStylusCount + 1];

    /// <summary>
    /// 最简单的画线模式
    /// </summary>
    public bool IsDrawLineMode { set; get; }

    public bool IsFullScreen { set; get; } = true;

    /// <summary>
    /// 进入橡皮擦模式
    /// </summary>
    public void EnterEraserMode()
    {
        _skInkCanvas.EnterEraserMode();
    }

    public void EnterPenMode()
    {
        _skInkCanvas.EnterPenMode();
    }

    public unsafe void Run(nint ownerWindowIntPtr)
    {
        XSetInputFocus(Display, Window, 0, IntPtr.Zero);

        var wmState = XInternAtom(Display, "_NET_WM_STATE", true);
        if (ownerWindowIntPtr != IntPtr.Zero)
        {
            // bing 如何设置X11里面两个窗口之间的层级关系
            // bing 如何编写代码设置X11里面两个窗口之间的层级关系，比如有 a 和 b 两个窗口，如何设置 a 窗口一定在 b 窗口上方？
            // 我们使用XSetTransientForHint函数将窗口a设置为窗口b的子窗口。这将确保窗口a始终在窗口b的上方
            XSetTransientForHint(Display, ownerWindowIntPtr, Window);

            var xev = new XEvent
            {
                ClientMessageEvent =
                {
                    type = XEventName.ClientMessage,
                    send_event = true,
                    window = ownerWindowIntPtr,
                    message_type = wmState,
                    format = 32,
                    ptr1 = IntPtr.Zero,
                    ptr2 = IntPtr.Zero,
                    ptr3 = IntPtr.Zero,
                    ptr4 = IntPtr.Zero
                }
            };
            XSendEvent(Display, RootWindow, false,
                new IntPtr((int) (EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
        }

        if (IsFullScreen)
        {
            // 下面是进入全屏
            var hintsPropertyAtom = XInternAtom(Display, "_MOTIF_WM_HINTS", true);
            XChangeProperty(Display, Window, hintsPropertyAtom, hintsPropertyAtom, 32, PropertyMode.Replace, new uint[5]
            {
                2, // flags : Specify that we're changing the window decorations.
                0, // functions
                0, // decorations : 0 (false) means that window decorations should go bye-bye.
                0, // inputMode
                0, // status
            }, 5);

            ChangeWMAtoms(false, XInternAtom(Display, "_NET_WM_STATE_HIDDEN", true));
            ChangeWMAtoms(true, XInternAtom(Display, "_NET_WM_STATE_FULLSCREEN", true));
            ChangeWMAtoms(false, XInternAtom(Display, "_NET_WM_STATE_MAXIMIZED_VERT", true), XInternAtom(Display, "_NET_WM_STATE_MAXIMIZED_HORZ", true));

            var topmostAtom = XInternAtom(Display, "_NET_WM_STATE_ABOVE", true);
            SendNetWMMessage(wmState, new IntPtr(1), topmostAtom);
        }

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

        // ABS_MT_TOUCH_MAJOR ABS_MT_TOUCH_MINOR
        // https://www.kernel.org/doc/html/latest/input/multi-touch-protocol.html
        var touchMajorAtom = XInternAtom(Display, "Abs MT Touch Major", false);
        var touchMinorAtom = XInternAtom(Display, "Abs MT Touch Minor", false);
        IntPtr pressureAtom = XInternAtom(Display, "Abs MT Pressure", false);

        Console.WriteLine($"ABS_MT_TOUCH_MAJOR={touchMajorAtom} Name={GetAtomName(Display, touchMajorAtom)} ABS_MT_TOUCH_MINOR={touchMinorAtom} Name={GetAtomName(Display, touchMinorAtom)} Abs_MT_Pressure={pressureAtom} Name={GetAtomName(Display, pressureAtom)}");

        var valuators = new List<XIValuatorClassInfo>();
        var scrollers = new List<XIScrollClassInfo>();

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

            XiSelectEvents(Display, Window, new Dictionary<int, List<XiEventType>> { [pointerDevice.Value.Deviceid] = eventTypes });

            Console.WriteLine($"pointerDevice.Value.NumClasses={pointerDevice.Value.NumClasses}");

            for (int i = 0; i < pointerDevice.Value.NumClasses; i++)
            {
                var xiAnyClassInfo = pointerDevice.Value.Classes[i];
                if (xiAnyClassInfo->Type == XiDeviceClass.XIValuatorClass)
                {
                    valuators.Add(*((XIValuatorClassInfo**) pointerDevice.Value.Classes)[i]);
                }
                else if (xiAnyClassInfo->Type == XiDeviceClass.XIScrollClass)
                {
                    scrollers.Add(*((XIScrollClassInfo**) pointerDevice.Value.Classes)[i]);
                }
            }

            //foreach (var xiValuatorClassInfo in valuators)
            //{
            //    var label = xiValuatorClassInfo.Label;
            //    // 不能通过 Marshal.PtrToStringAnsi 读取 Label 的值 读取不到
            //    //Marshal.PtrToStringAnsi(xiValuatorClassInfo.Label);
            //    Console.WriteLine($"[Valuator] [{GetAtomName(Display, label)}] Label={label} Type={xiValuatorClassInfo.Type} Sourceid={xiValuatorClassInfo.Sourceid} Number={xiValuatorClassInfo.Number} Min={xiValuatorClassInfo.Min} Max={xiValuatorClassInfo.Max} Value={xiValuatorClassInfo.Value} Resolution={xiValuatorClassInfo.Resolution} Mode={xiValuatorClassInfo.Mode}");
            //}
        }

        var skInkCanvas = _skInkCanvas;
        skInkCanvas.Settings = skInkCanvas.Settings with
        {
            AutoSoftPen = false,
        };
        skInkCanvas.SetCanvas(_skCanvas);
        skInkCanvas.RenderBoundsChanged += (sender, rect) =>
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
        };

        while (true)
        {
            var xNextEvent = XNextEvent(Display, out var @event);
            if (xNextEvent != 0)
            {
                break;
            }

            if (@event.type == XEventName.Expose)
            {
                // 曝光时，可以收到需要重新绘制的范围
                XPutImage(Display, Window, GC, ref _image, @event.ExposeEvent.x, @event.ExposeEvent.y, @event.ExposeEvent.x, @event.ExposeEvent.y, (uint) @event.ExposeEvent.width,
                    (uint) @event.ExposeEvent.height);
            }
            else if (@event.type == XEventName.ClientMessage)
            {
                Console.WriteLine($"XEventName.ClientMessage ===============");
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

        //Console.WriteLine($"Count={_stylusPoints.Count}");

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

    public SKColor Color
    {
        set => _skInkCanvas.Color = value;
        get => _skInkCanvas.Color;
    }

    private (int X, int Y) _lastPoint;
    private bool _isDown;
    private readonly SKBitmap _skBitmap;
    private readonly SKCanvas _skCanvas;

    private SkInkCanvas _skInkCanvas;

    //private readonly SKSurface _skSurface;

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

        var img = new XImage();
        int bitsPerPixel = bytePerPixelCount * bitPerByte;
        img.width = _skBitmap.Width;
        img.height = _skBitmap.Height;
        img.format = 2; //ZPixmap;
        img.data = _skBitmap.GetPixels();
        img.byte_order = 0; // LSBFirst;
        img.bitmap_unit = bitsPerPixel;
        img.bitmap_bit_order = 0; // LSBFirst;
        img.bitmap_pad = bitsPerPixel;
        img.depth = bitsPerPixel;
        img.bytes_per_line = _skBitmap.Width * bytePerPixelCount;
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