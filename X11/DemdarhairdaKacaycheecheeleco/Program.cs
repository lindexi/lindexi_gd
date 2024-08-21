// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CPF.Linux;

using static CPF.Linux.XLib;
using static CPF.Linux.XShm;
using static CPF.Linux.LibC;
using System.Runtime.InteropServices;
using SkiaSharp;

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

    var gc = XCreateGC(display, handle, 0, 0);

    XFlush(display);

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

    var stopwatch = new Stopwatch();

    var mapLength = width * 4 * height;
    //Console.WriteLine($"Length = {mapLength}");

    //(IntPtr shmImage, IntPtr shmAddr, IntPtr debugIntPtr) Init()
    //{
    //    Span<byte> span = stackalloc byte[1024];
    //    Random.Shared.NextBytes(span);

    //    var xShmInfo = CreateXShmInfo(display, visual, width, height, mapLength);

    //    return (xShmInfo.ShmAddr, (IntPtr) xShmInfo.ShmImage, xShmInfo.DebugIntPtr);
    //}

    //var (shmImage, shmAddr, debugIntPtr) = Init();

    //var xShmInfo = CreateXShmInfo(display, visual, width, height, mapLength);
    //var (shmImage, shmAddr, debugIntPtr) = (xShmInfo.ShmAddr, (IntPtr) xShmInfo.ShmImage, xShmInfo.DebugIntPtr);

    var foo = new Foo();
    var c = &foo.Value;
    c[0] = 0xCC;
    Console.WriteLine($"内存Pc={new IntPtr(c):X}");

    var xShmProvider = new XShmProvider(new RenderInfo(display, visual, width, height, mapLength), new IntPtr(c));
    var xShmInfo = xShmProvider.XShmInfo;
    var (shmImage, shmAddr, debugIntPtr) = (xShmInfo.ShmAddr, (IntPtr) xShmInfo.ShmImage, xShmInfo.DebugIntPtr);

    void Draw()
    {
        Span<byte> span = stackalloc byte[1024];
        //Random.Shared.NextBytes(span);
        var sharedMemory = (byte*) shmAddr;

        for (int i = 0; i < span.Length; i++)
        {
            sharedMemory[i] = span[i];
        }

        Console.WriteLine($"绘制完成");
    }

    //Draw();

   
    var d = new IntPtr(c).ToInt64() - debugIntPtr.ToInt64();
    Console.WriteLine($"Pc={new IntPtr(c):X} 调试距离={d}");
    for (int i = 0; i < 1024 * 2; i++)
    {
        c[i] = 0xCC;
    }
    Console.WriteLine($"Pc={new IntPtr(c + 2047):X}");

    while (true)
    {
        var xNextEvent = XNextEvent(display, out var @event);

        if (xNextEvent != 0)
        {
            break;
        }

        if (@event.type == XEventName.Expose)
        {


            // 模拟绘制界面
            //Draw();
            //Span<byte> span = new Span<byte>((&foo.Value), 1024 * 2);
            //var sharedMemory = (byte*) shmAddr;
            //for (int i = 0; i < span.Length; i++)
            //{
            //    sharedMemory[i] = span[i];
            //}
            //Console.WriteLine($"绘制完成");

            stopwatch.Restart();

            Console.WriteLine($"当前调试代码的内存 {*((long*) debugIntPtr):X}");

            XShmPutImage(display, handle, gc, (XImage*) shmImage, 0, 0, 0, 0, (uint) width, (uint) height, true);

            XFlush(display);

            stopwatch.Stop();
        }
        else if ((int) @event.type == 65 /*XShmCompletionEvent*/)
        {
        }
    }
}

Console.WriteLine("Hello, World!");



public record RenderInfo(IntPtr Display, nint Visual, int Width, int Height, int DataByteLength);

class XShmProvider
{
    public XShmProvider(RenderInfo renderInfo, IntPtr debugIntPtr)
    {
        _renderInfo = renderInfo;
        _debugIntPtr = debugIntPtr;
        XShmInfo = Init();
    }

    public XShmInfo XShmInfo { get; }
    private readonly RenderInfo _renderInfo;
    private readonly IntPtr _debugIntPtr;

    private XShmInfo Init()
    {
        //// 尝试抬高栈的空间
        //Span<byte> span = stackalloc byte[1024];
        //Random.Shared.NextBytes(span);

        var renderInfo = _renderInfo;
        var result = CreateXShmInfo(renderInfo.Display, renderInfo.Visual, renderInfo.Width, renderInfo.Height, renderInfo.DataByteLength);
        return result;
    }

    static unsafe XShmInfo CreateXShmInfo(IntPtr display, nint visual, int width, int height, int mapLength)
    {
        var status = XShmQueryExtension(display);
        if (status == 0)
        {
            throw new Exception("XShmQueryExtension failed"); // 实际使用请换成你的业务异常类型
        }

        status = XShmQueryVersion(display, out var major, out var minor, out var pixmaps);
        Console.WriteLine($"XShmQueryVersion: {status} major={major} minor={minor} pixmaps={pixmaps}");
        if (status == 0)
        {
            throw new Exception("XShmQueryVersion failed"); // 实际使用请换成你的业务异常类型
        }

        const int ZPixmap = 2;
        var xShmSegmentInfo = new XShmSegmentInfo();
        var shmImage = (XImage*) XShmCreateImage(display, visual, 32, ZPixmap, IntPtr.Zero, &xShmSegmentInfo,
            (uint) width, (uint) height);

        Console.WriteLine($"XShmCreateImage = {(IntPtr) shmImage:X} xShmSegmentInfo={xShmSegmentInfo} PXShmCreateImage={new IntPtr(&xShmSegmentInfo):X}");

        var shmgetResult = shmget(IPC_PRIVATE, mapLength, IPC_CREAT | 0777);
        Console.WriteLine($"shmgetResult={shmgetResult:X}");

        xShmSegmentInfo.shmid = shmgetResult;

        var shmaddr = shmat(shmgetResult, IntPtr.Zero, 0);
        Console.WriteLine($"shmaddr={shmaddr:X}");

        xShmSegmentInfo.shmaddr = (char*) shmaddr.ToPointer();
        shmImage->data = shmaddr;

        XShmAttach(display, &xShmSegmentInfo);
        XFlush(display);

        return new XShmInfo(shmImage, shmaddr)
        {
            DebugIntPtr = new IntPtr(&xShmSegmentInfo)
        };
    }
}



[InlineArray(1024 * 2)]
struct Foo
{
    public byte Value;
}

unsafe class XShmInfo
{
    public XShmInfo(XImage* shmImage, IntPtr shmAddr)
    {
        ShmImage = shmImage;
        ShmAddr = shmAddr;
    }

    public XImage* ShmImage { get; }

    public IntPtr ShmAddr { get; }

    public IntPtr DebugIntPtr { set; get; }
}