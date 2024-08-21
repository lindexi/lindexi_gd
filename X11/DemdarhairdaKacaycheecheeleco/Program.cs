// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using CPF.Linux;
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
        (int)CreateWindowArgs.InputOutput,
        visual,
        (nuint)valueMask, ref xSetWindowAttributes);

    XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                             XEventMask.PointerMotionHintMask;
    var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
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
                new IntPtr((int)(EventMask.ExposureMask)),
                ref xEvent);

            XFlush(newDisplay);
        }

        XCloseDisplay(newDisplay);
    });

    var stopwatch = new Stopwatch();

    // 一个像素占用4个字节，于是总共的字节数就是 width * 4 * height 的长度
    var mapLength = width * 4 * height;
    //Console.WriteLine($"Length = {mapLength}");

    var renderInfo = new RenderInfo(display, visual, width, height, mapLength, handle, gc);
    var xShmProvider = new XShmProvider(renderInfo);
    while (true)
    {
        var xNextEvent = XNextEvent(display, out var @event);

        if (xNextEvent != 0)
        {
            break;
        }

        if (@event.type == XEventName.Expose)
        {
            stopwatch.Restart();

            xShmProvider.DoDraw();

            stopwatch.Stop();
        }
        else if ((int)@event.type == 65 /*XShmCompletionEvent*/)
        {
        }
    }
}

Console.WriteLine("Hello, World!");

public record RenderInfo
(
    IntPtr Display,
    IntPtr Visual,
    int Width,
    int Height,
    int DataByteLength,
    IntPtr Handle,
    IntPtr GC
);

class XShmProvider
{
    public XShmProvider(RenderInfo renderInfo)
    {
        _renderInfo = renderInfo;
        XShmInfo = Init();
    }

    public XShmInfo XShmInfo { get; }
    private readonly RenderInfo _renderInfo;

    private XShmInfo Init()
    {
        // 尝试抬高栈的空间
        // 用于让 XShmSegmentInfo 的内存地址不被后续压入方法栈的数据覆盖
        Span<byte> span = stackalloc byte[1024];
        Random.Shared.NextBytes(span);

        var renderInfo = _renderInfo;
        var result = CreateXShmInfo(renderInfo.Display, renderInfo.Visual, renderInfo.Width, renderInfo.Height,
            renderInfo.DataByteLength);
        return result;
    }

    private static unsafe XShmInfo CreateXShmInfo(IntPtr display, nint visual, int width, int height, int mapLength)
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
        // 核心问题就是 XShmSegmentInfo 是结构体，在这里将在栈上分配。后续将使用栈空间的地址传递给 XShmCreateImage 方法，然而在此方法执行之后，将会弹栈，导致 XShmSegmentInfo 的内存地址被覆盖。从而让 XImage 里面记录的 obdata 字段指向错误的地址，导致后续的 XShmPutImage 方法无法正确的使用共享内存，输出如下错误
        // X Error of failed request:  BadShmSeg (invalid shared segment parameter)
        //   Major opcode of failed request:  130 (MIT-SHM)
        //   Minor opcode of failed request:  3 (X_ShmPutImage)
        //   Segment id in failed request:  0x0
        //   Serial number of failed request:  17
        //   Current serial number in output stream:  17
        // 上述错误的 `Segment id in failed request:  0x0` 就说明了问题，即 XImage 里面记录的 obdata 字段指向了 0x0 的地址。常见的错误就是类似野指针问题或者指针被覆盖的问题
        // 在本例中，我们将 XShmSegmentInfo 的在栈上分配的内存地址给到 XImage 里面记录的 obdata 字段，方法结束之后，栈空间被覆盖，导致 obdata 字段指向了错误的地址
        // 为什么刚好是 0x0 的地址呢？其实原因在于后续的 DoDraw 使用 Span<byte> span = stackalloc byte[1024 * 2]; 强行申请更多的栈空间，从而覆盖到了 XShmSegmentInfo 的内存地址。如果非 DoDraw 强行申请且保持默认为 0 的填充，则这里的错误信息 Segment id in failed request 的值会更加迷惑，甚至指向的是一个随机的地址导致 Segmentation fault (core dumped) 段错误或 The RX block to map as RW was not found 或 The RW block to unmap was not found 或 corrupted double-linked list 等
        var xShmSegmentInfo = new XShmSegmentInfo();
        var shmImage = (XImage*)XShmCreateImage(display, visual, 32, ZPixmap, IntPtr.Zero, &xShmSegmentInfo,
            (uint)width, (uint)height);

        Console.WriteLine(
            $"XShmCreateImage = {(IntPtr)shmImage:X} xShmSegmentInfo={xShmSegmentInfo} PXShmCreateImage={new IntPtr(&xShmSegmentInfo):X}");

        var shmgetResult = shmget(IPC_PRIVATE, mapLength, IPC_CREAT | 0777);
        Console.WriteLine($"shmgetResult={shmgetResult:X}");

        xShmSegmentInfo.shmid = shmgetResult;

        var shmaddr = shmat(shmgetResult, IntPtr.Zero, 0);
        Console.WriteLine($"shmaddr={shmaddr:X}");

        xShmSegmentInfo.shmaddr = (char*)shmaddr.ToPointer();
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
        // 申请两倍于压栈空间的大小，确保测试地址被覆盖到，从而能够复现问题
        Span<byte> span = stackalloc byte[1024 * 2];
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = 0x00;
        }

        Console.WriteLine($"当前调试代码的内存 {*((long*)XShmInfo.DebugIntPtr):X}");

        var display = _renderInfo.Display;
        var handle = _renderInfo.Handle;
        var gc = _renderInfo.GC;
        var shmImage = XShmInfo.ShmImage;
        var width = _renderInfo.Width;
        var height = _renderInfo.Height;

        XShmPutImage(display, handle, gc, (XImage*)shmImage, 0, 0, 0, 0, (uint)width, (uint)height, true);

        XFlush(display);
    }
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