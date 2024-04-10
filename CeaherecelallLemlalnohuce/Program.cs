using CeaherecelallLemlalnohuce;

XLib.XInitThreads();
var display = XLib.XOpenDisplay(0);
var screen = XLib.XDefaultScreen(display);
var defaultScreen = XLib.XDefaultScreen(display);
var rootWindow = XLib.XRootWindow(display, defaultScreen);
XLib.XMatchVisualInfo(display, screen, 32, 4, out var info);
var visual = info.visual;
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
    override_redirect = false, // 参数：_overrideRedirect
    colormap = XLib.XCreateColormap(display, rootWindow, visual, 0),
};

var window = XLib.XCreateWindow(display, rootWindow, 100, 100, 320, 240, 0,
    32,
    (int)CreateWindowArgs.InputOutput,
    visual,
    (nuint)valueMask, ref attr);

XEventMask ignoredMask = XEventMask.SubstructureRedirectMask | XEventMask.ResizeRedirectMask |
                         XEventMask.PointerMotionHintMask;
var mask = new IntPtr(0xffffff ^ (int)ignoredMask);
XLib.XSelectInput(display, window, mask);

var gc = XLib.XCreateGC(display, window, 0, 0);

XLib.XMapWindow(display, window);
XLib.XFlush(display);

#region 全屏 最顶层

var wmState = XLib.XInternAtom(display, "_NET_WM_STATE", true);

ChangeWMAtoms(false, XLib.XInternAtom(display, "_NET_WM_STATE_HIDDEN", true));
ChangeWMAtoms(true, XLib.XInternAtom(display, "_NET_WM_STATE_FULLSCREEN", true));
ChangeWMAtoms(false, XLib.XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_VERT", true),
    XLib.XInternAtom(display, "_NET_WM_STATE_MAXIMIZED_HORZ", true));

// 最顶层 类似 WPF 的 Topmost 功能
ChangeWMAtoms(true, XLib.XInternAtom(display, "_NET_WM_STATE_ABOVE", true));

void ChangeWMAtoms(bool enable, params IntPtr[] atoms)
{
    var xev = new XEvent
    {
        ClientMessageEvent =
        {
            type = XEventName.ClientMessage,
            send_event = true,
            window = window,
            message_type = wmState,
            format = 32,
            ptr1 = new IntPtr(enable ? 1 : 0),
            ptr2 = (IntPtr?)atoms[0] ?? IntPtr.Zero,
            ptr3 = (IntPtr?)(atoms.Length > 1 ? atoms[1] : IntPtr.Zero) ?? IntPtr.Zero,
            ptr4 = (IntPtr?)(atoms.Length > 2 ? atoms[2] : IntPtr.Zero) ?? IntPtr.Zero
        }
    };
    XLib.XSendEvent(display, rootWindow, false,
        new IntPtr((int)(EventMask.SubstructureRedirectMask | EventMask.SubstructureNotifyMask)), ref xev);
}

#endregion

var white = XLib.XWhitePixel(display, screen);
var black = XLib.XBlackPixel(display, screen);
XLib.XSetForeground(display, gc, white);
var xDisplayWidth = XLib.XDisplayWidth(display, screen);
var xDisplayHeight = XLib.XDisplayHeight(display, screen);

while (XLib.XNextEvent(display, out var xEvent) == default)
{
    if (xEvent.type == XEventName.Expose)
    {
        XLib.XDrawLine(display, window, gc, 0, 0, xDisplayWidth, xDisplayHeight);
        XLib.XDrawLine(display, window, gc, 0, xDisplayHeight, xDisplayWidth, 0);
    }
}

XLib.XUnmapWindow(display, window);
XLib.XDestroyWindow(display, window);