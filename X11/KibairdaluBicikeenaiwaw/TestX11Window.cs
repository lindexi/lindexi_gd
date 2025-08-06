using CPF.Linux;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CPF.Linux.XLib;

namespace KibairdaluBicikeenaiwaw;

internal class TestX11Window
{
    public TestX11Window(int x, int y, int width, int height, nint display, nint rootWindow, int screen,
        bool isFullScreen)
    {
        Display = display;

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

        var handle = XCreateWindow(display, rootWindow, x, y, width, height, 5,
            32,
            (int)CreateWindowArgs.InputOutput,
            visual,
            (nuint)valueMask, ref xSetWindowAttributes);

        X11Window = handle;

        XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                                 XEventMask.PointerMotionHintMask;
        var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
        XSelectInput(display, handle, mask);

        var gc = XCreateGC(display, handle, 0, 0);
        GC = gc;

        X = x;
        Y = y;

        Width = width;
        Height = height;

        RootWindow = rootWindow;

        //if (isFullScreen)
        //{
        //    SetFullScreenMonitor();
        //}
    }

    public void SetFullScreen()
    {
        var hiddenAtom = XInternAtom(Display, "_NET_WM_STATE_HIDDEN", true);
        var fullScreenAtom = XInternAtom(Display, "_NET_WM_STATE_FULLSCREEN", true);

        ChangeWMAtoms(false, hiddenAtom);
        ChangeWMAtoms(true, fullScreenAtom);
        //ChangeWMAtoms(false, _x11.Atoms._NET_WM_STATE_MAXIMIZED_VERT,
        //    _x11.Atoms._NET_WM_STATE_MAXIMIZED_HORZ);
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
                window = X11Window,
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

    public void SetFullScreenMonitor()
    {
        // [Window Manager Protocols | Extended Window Manager Hints](https://specifications.freedesktop.org/wm-spec/1.5/ar01s06.html )
        // 6.3 _NET_WM_FULLSCREEN_MONITORS

        var wmState = XInternAtom(Display, "_NET_WM_FULLSCREEN_MONITORS", true);
        Console.WriteLine($"_NET_WM_FULLSCREEN_MONITORS={wmState}");

        // _NET_WM_FULLSCREEN_MONITORS, CARDINAL[4]/32
        /*
         data.l[0] = the monitor whose top edge defines the top edge of the fullscreen window
         data.l[1] = the monitor whose bottom edge defines the bottom edge of the fullscreen window
         data.l[2] = the monitor whose left edge defines the left edge of the fullscreen window
         data.l[3] = the monitor whose right edge defines the right edge of the fullscreen window
        */
        var left = X;
        var top = Y;
        var right = X + Width;
        var bottom = Y + Height;

        Console.WriteLine($"Left={left} Top={top} Right={right} Bottom={bottom}");

        int[] monitorEdges = [top, bottom, left, right];
        XChangeProperty(Display, X11Window, wmState, (IntPtr) Atom.XA_CARDINAL, format: 32, PropertyMode.Replace,
            monitorEdges, monitorEdges.Length);

        // A Client wishing to change this list MUST send a _NET_WM_FULLSCREEN_MONITORS client message to the root window. The Window Manager MUST keep this list updated to reflect the current state of the window.
        var xev = new XEvent
        {
            ClientMessageEvent =
            {
                type = XEventName.ClientMessage,
                send_event = true,
                window = X11Window,
                message_type = wmState,
                format = 32,
                ptr1 = top,
                ptr2 = bottom,
                ptr3 = left,
                ptr4 = right,
            }
        };

        XSendEvent(Display, RootWindow, false,
            new IntPtr((int) (EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
    }

    public IntPtr X11Window { get; }

    public IntPtr Display { get; }
    public IntPtr GC { get; }

    public int X { get; }
    public int Y { get; }

    public int Width { get; }
    public int Height { get; }

    public IntPtr RootWindow { get; }

    public void MapWindow()
    {
        XMapWindow(Display, X11Window);
        XFlush(Display);
    }

    public void Draw()
    {
        using var skBitmap = new SKBitmap(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using var skCanvas = new SKCanvas(skBitmap);

        var xImage = CreateImage(skBitmap);

        skCanvas.Clear(new SKColor((uint)Random.Shared.Next()).WithAlpha(0xFF));
        skCanvas.Flush();

        XPutImage(Display, X11Window, GC, ref xImage, 0, 0, 0, 0, (uint)skBitmap.Width,
            (uint)skBitmap.Height);
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
}