using System.Collections.Immutable;

using BlankX11App.X11;

using static BlankX11App.X11.XLib;
using static BlankX11App.X11.GlxConsts;
using System.Diagnostics;

//while (true)
//{
//    Thread.Sleep(100);
//    if (Debugger.IsAttached)
//    {
//        break;
//    }
//}

var display = XOpenDisplay(0);
var defaultScreen = XDefaultScreen(display);
var rootWindow = XRootWindow(display, defaultScreen);
var visual = GetVisual(XOpenDisplay(0), defaultScreen);
var valueMask = SetWindowValuemask.BackPixmap
    | SetWindowValuemask.BackPixel
    | SetWindowValuemask.BorderPixel
    | SetWindowValuemask.BitGravity
    | SetWindowValuemask.WinGravity
    | SetWindowValuemask.BackingStore
    | SetWindowValuemask.ColorMap;
var attr = new XSetWindowAttributes
{
    backing_store = 1,
    bit_gravity = Gravity.NorthWestGravity,
    win_gravity = Gravity.NorthWestGravity,
    override_redirect = 0,  // 参数：_overrideRedirect
    colormap = XCreateColormap(display, rootWindow, visual, 0),
};

var handle = XCreateWindow(display, rootWindow, 100, 100, 320, 240, 0,
    32,
    (int)CreateWindowArgs.InputOutput,
    visual,
    (nuint)valueMask, ref attr);
XMapWindow(display, handle);
XFlush(display);

while (XNextEvent(display, out var xEvent) == default)
{
}

XUnmapWindow(display, handle);
XDestroyWindow(display, handle);

static unsafe nint GetVisual(nint deferredDisplay, int defaultScreen)
{
    var glx = new GlxInterface();
    nint fbconfig = 0;
    XVisualInfo* visual = null;
    int[] baseAttribs =
        [
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
        ];

    foreach (var attribs in new[] { baseAttribs })
    {
        var ptr = glx.ChooseFBConfig(deferredDisplay, defaultScreen,
            attribs, out var count);
        for (var c = 0; c < count; c++)
        {

            var v = glx.GetVisualFromFBConfig(deferredDisplay, ptr[c]);
            // We prefer 32 bit visuals
            if (fbconfig == default || v->depth == 32)
            {
                fbconfig = ptr[c];
                visual = v;
                if (v->depth == 32)
                {
                    break;
                }
            }
        }

        if (fbconfig != default)
        {
            break;
        }
    }

    return visual->visual;
}
