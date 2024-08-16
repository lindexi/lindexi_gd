using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CPF.Linux;
using ShmSeg = System.UInt64;
using static CPF.Linux.XLib;

namespace WercawchallwarnefeWhedurcachay;
internal unsafe class XShm
{
    [DllImport("libXext.so.6", SetLastError = true)]
    static extern int XShmQueryExtension(IntPtr display);

    /*
 Status XShmQueryVersion (display, major, minor, pixmaps)
   Display *display;
   int *major, *minor;
   Bool *pixmaps
 */
    [DllImport("libXext.so.6", SetLastError = true)]
    static extern int XShmQueryVersion(IntPtr display, out int major, out int minor, out bool pixmaps);

    public static void Run()
    {
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

        var mapLength = width * 4 * height;
        //Console.WriteLine($"Length = {mapLength}");


        var status = XShmQueryExtension(display);
        if (status == 0)
        {
            Console.WriteLine("XShmQueryExtension failed");
        }

        status = XShmQueryVersion(display, out var major, out var minor, out var pixmaps);
        Console.WriteLine($"XShmQueryVersion: {status} major={major} minor={minor} pixmaps={pixmaps}");

        // /* Create XImage structure and map image memory on it */
        // ximage = XShmCreateImage(display, DefaultVisual(display, 0), DefaultDepth(display, 0), ZPixmap, 0, &shminfo, 100, 100);
        const int ZPixmap = 2;
        var xShmSegmentInfo = new XShmSegmentInfo();
        var shmImage = (XImage*) XShmCreateImage(display, visual, 32, ZPixmap, IntPtr.Zero, &xShmSegmentInfo, (uint) width, (uint) height);

        Console.WriteLine($"XShmCreateImage = {(IntPtr)shmImage:X} xShmSegmentInfo={xShmSegmentInfo}");

        var shmgetResult = shmget(IPC_PRIVATE, mapLength, IPC_CREAT | 0777);
        Console.WriteLine($"shmgetResult={shmgetResult:X}");

        xShmSegmentInfo.shmid = shmgetResult;

        var shmaddr = shmat(shmgetResult, IntPtr.Zero, 0);
        Console.WriteLine($"shmaddr={shmaddr:X}");

        xShmSegmentInfo.shmaddr = (char*) shmaddr.ToPointer();
        shmImage->data = shmaddr;



        var XShmAttachResult = XShmAttach(display, &xShmSegmentInfo);
        XFlush(display);
        Console.WriteLine($"完成 XShmAttach XShmAttachResult={XShmAttachResult}");

        XShmPutImage(display, handle, gc, (XImage*) shmImage, 0, 0, 0, 0, (uint) width, (uint) height, false);

        XFlush(display);

        Console.WriteLine($"完成推送图片");

        while (true)
        {
            var xNextEvent = XNextEvent(display, out var @event);

            if (xNextEvent != 0)
            {
                break;
            }
        }
    }

    [DllImport("libXext.so.6", SetLastError = true)]
    static extern int XShmPutImage(IntPtr display, IntPtr drawable, IntPtr gc, XImage* image, int src_x, int src_y, int dst_x, int dst_y, uint src_width, uint src_height, bool send_event);

    // XShmAttach(display, &shminfo);
    [DllImport("libXext.so.6", SetLastError = true)]
    static extern int XShmAttach(IntPtr display, XShmSegmentInfo* shminfo);

    [DllImport("libc", SetLastError = true)]
    static extern IntPtr shmat(int shmid, IntPtr shmaddr, int shmflg);

    //    #define IPC_CREAT	01000		/* create key if key does not exist */
    // #define IPC_PRIVATE	((key_t) 0)	/* private key */

    const int IPC_CREAT = 01000;
    const int IPC_PRIVATE = 0;

    /*
 * /* Setting SHM * /
      shminfo.shmid = shmget(IPC_PRIVATE, 100 * 100 * 4, IPC_CREAT | 0777);
 */

    /*
     * int shmget(key_t key, size_t size, int shmflg);
     */
    [DllImport("libc", SetLastError = true)]
    static extern int shmget(int key, IntPtr size, int shmflg);

    /*
 XImage *XShmCreateImage (display, visual, depth, format, data,
                    shminfo, width, height)
   Display *display;
   Visual *visual;
   unsigned int depth, width, height;
   int format;
   char *data;
   XShmSegmentInfo *shminfo;
 */
    [DllImport("libXext.so.6", SetLastError = true)]
    static extern IntPtr XShmCreateImage(IntPtr display, IntPtr visual, uint depth, int format, IntPtr data,  XShmSegmentInfo* shminfo, uint width, uint height);
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct XShmSegmentInfo
{
   public ShmSeg shmseg;    /* resource id */
   public int shmid;        /* kernel id */
   public char* shmaddr;    /* address in client */
   public bool readOnly;	/* how the server should attach it */

   public override string ToString()
   {
       return    $"XShmSegmentInfo {{ shmseg = {shmseg}, shmid = {shmid}, shmaddr = {new IntPtr(shmaddr).ToString("X")}, readOnly = {readOnly} }}";
   }
}