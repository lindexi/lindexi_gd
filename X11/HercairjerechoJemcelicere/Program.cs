// See https://aka.ms/new-console-template for more information
using static CPF.Linux.XLib;
using Avalonia.OpenGL.Egl;
using CPF.Linux;
using System.ComponentModel;
using Avalonia.OpenGL;
using Avalonia.X11.Glx;
using XLib = Avalonia.X11.XLib;
using static Avalonia.X11.Glx.GlxConsts;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

XMatchVisualInfo(display, screen, 32, 4, out var info);
//var visual = info.visual;

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
    background_pixel = new IntPtr(0x65565656),
};

var xDisplayWidth = XDisplayWidth(display, screen) / 2;
var xDisplayHeight = XDisplayHeight(display, screen) / 2;
var handle = XCreateWindow(display, rootWindow, 0, 0, xDisplayWidth, xDisplayHeight, 5,
    32,
    (int) CreateWindowArgs.InputOutput,
    visual,
    (nuint) valueMask, ref xSetWindowAttributes);

XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int) ignoredMask);
XSelectInput(display, handle, mask);

GlVersion[] GlProfiles = new[]
{
    new GlVersion(GlProfileType.OpenGL, 4, 0),
    new GlVersion(GlProfileType.OpenGL, 3, 2),
    new GlVersion(GlProfileType.OpenGL, 3, 0),
    new GlVersion(GlProfileType.OpenGLES, 3, 2),
    new GlVersion(GlProfileType.OpenGLES, 3, 0),
    new GlVersion(GlProfileType.OpenGLES, 2, 0)
};
GlxInterface Glx = new GlxInterface();
var _probeProfiles = GlProfiles;
var _displayExtensions = Glx.GetExtensions(display);
var baseAttribs = new[]
{
    GLX_X_RENDERABLE, 1,
    GLX_RENDER_TYPE, GLX_RGBA_BIT,
    GLX_DRAWABLE_TYPE, GLX_WINDOW_BIT | GLX_PBUFFER_BIT,
    GLX_DOUBLEBUFFER, 1,
    GLX_RED_SIZE, 8,
    GLX_GREEN_SIZE, 8,
    GLX_BLUE_SIZE, 8,
    GLX_ALPHA_SIZE, 8,
    GLX_DEPTH_SIZE, 1,
    GLX_STENCIL_SIZE, 8,
};

unsafe
{
    int sampleCount = 0;
    int stencilSize = 0;
    foreach (var attribs in new[]
         {
             //baseAttribs.Concat(multiattribs),
             baseAttribs,
         })
    {
        var ptr = Glx.ChooseFBConfig(_x11.DeferredDisplay, x11.DefaultScreen,
            attribs, out var count);
        for (var c = 0; c < count; c++)
        {
            var visual = Glx.GetVisualFromFBConfig(_x11.DeferredDisplay, ptr[c]);
            // We prefer 32 bit visuals
            if (_fbconfig == IntPtr.Zero || visual->depth == 32)
            {
                _fbconfig = ptr[c];
                _visual = visual;
                if (visual->depth == 32)
                    break;
            }
        }

        if (_fbconfig != IntPtr.Zero)
            break;
    }



    XMapWindow(display, handle);
    XFlush(display);


    var white = XWhitePixel(display, screen);
    var black = XBlackPixel(display, screen);

    var gc = XCreateGC(display, handle, 0, 0);
    XSetForeground(display, gc, white);
    XSync(display, false);

    Console.WriteLine("Hello, World!");

    while (true)
    {
        var xNextEvent = XNextEvent(display, out var @event);
    }
}