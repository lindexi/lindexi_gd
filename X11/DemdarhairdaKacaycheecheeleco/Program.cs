// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CPF.Linux;

using static CPF.Linux.XLib;
using static CPF.Linux.XShm;
using static CPF.Linux.LibC;
using System.Runtime.InteropServices;
using SkiaSharp;
using System.Reflection.Metadata;
using System;

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

    //var foo = new Foo();
    //var c = &foo.Value;
    //c[0] = 0xCC;
    //Console.WriteLine($"内存Pc={new IntPtr(c):X}");

    //var foo2 = new Foo();
    //var c2 = &foo2.Value;
    //Console.WriteLine($"内存Pc2={new IntPtr(c2):X} {new IntPtr(c2).ToInt64() - new IntPtr(c).ToInt64()}");

    var xShmProvider = new XShmProvider(new RenderInfo(display, visual, width, height, mapLength,handle,gc), new IntPtr());
    xShmProvider.DoDraw();
    var xShmInfo = xShmProvider.XShmInfo;
    var (shmImage, shmAddr, debugIntPtr) = (xShmInfo.ShmAddr, (IntPtr) xShmInfo.ShmImage, xShmInfo.DebugIntPtr);

    //void Draw()
    //{
    //    Span<byte> span = stackalloc byte[1024];
    //    //Random.Shared.NextBytes(span);
    //    var sharedMemory = (byte*) shmAddr;

    //    for (int i = 0; i < span.Length; i++)
    //    {
    //        sharedMemory[i] = span[i];
    //    }

    //    Console.WriteLine($"绘制完成");
    //}

    //Draw();

    //// 在优化中，被提升到前面执行了
    //var d = new IntPtr(c).ToInt64() - debugIntPtr.ToInt64();
    //Console.WriteLine($"Pc={new IntPtr(c):X} 调试距离={d}");
    //for (int i = 0; i < 1024 * 2; i++)
    //{
    //    c[i] = 0xCC;
    //}
    //Console.WriteLine($"Pc={new IntPtr(c + 2047):X}");

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


          

            stopwatch.Stop();
        }
        else if ((int) @event.type == 65 /*XShmCompletionEvent*/)
        {
        }
    }
}

Console.WriteLine("Hello, World!");



public record RenderInfo(
    IntPtr Display,
    IntPtr Visual,
    int Width,
    int Height,
    int DataByteLength,
    IntPtr Handle,
    IntPtr GC);

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
        // 尝试抬高栈的空间
        Span<byte> span = stackalloc byte[1024];
        Random.Shared.NextBytes(span);

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

    public unsafe void DoDraw()
    {
        Span<byte> span = stackalloc byte[1024*2];
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = 0x00;
        }

        Console.WriteLine($"当前调试代码的内存 {*((long*) XShmInfo.DebugIntPtr):X}");

        var display = _renderInfo.Display;
        var handle = _renderInfo.Handle;
        var gc = _renderInfo.GC;
        var shmImage = XShmInfo.ShmImage;
        var width = _renderInfo.Width;
        var height = _renderInfo.Height;

        XShmPutImage(display, handle, gc, (XImage*) shmImage, 0, 0, 0, 0, (uint) width, (uint) height, true);

        XFlush(display);
    }

    //public unsafe void DoDraw()
    //{
    //    var foo = new Foo();
    //    var c = &foo.Value;
    //    c[0] = 0xCC;
    //    Console.WriteLine($"DoDraw Pc={new IntPtr(c):X} _XShmInfo={XShmInfo.DebugIntPtr:X} 距离={new IntPtr(c).ToInt64() - XShmInfo.DebugIntPtr.ToInt64()} 当前调试代码的内存 {*((long*) XShmInfo.DebugIntPtr):X}");

    //    // 如果经过以下写入过程，那原先的 XShmSegmentInfo 所在的内存地址就会被覆盖，控制抬输出的内容如下
    //    // DoDraw Pc=7FCF54F7F8 _XShmInfo=7FCF54F2B8 距离=1344 当前调试代码的内存 CCCCCCCCCCCCCCCC
    //    //for (int i = 0; i < 1024 * 2; i++)
    //    //{
    //    //    *c = 0xCC;
    //    //    c++;
    //    //}
    //    //Console.WriteLine($"DoDraw Pc={new IntPtr(c):X} _XShmInfo={XShmInfo.DebugIntPtr:X} 距离={new IntPtr(c).ToInt64() - XShmInfo.DebugIntPtr.ToInt64()} 当前调试代码的内存 {*((long*) XShmInfo.DebugIntPtr):X}");
    //}
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