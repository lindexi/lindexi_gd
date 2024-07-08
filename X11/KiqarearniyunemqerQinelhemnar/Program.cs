using CPF.Linux;

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime;

using static CPF.Linux.XLib;

var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

var x11Window =
XCreateSimpleWindow(display, rootWindow, 100, 100, 500, 500, 1, IntPtr.Zero, IntPtr.Zero);

XSelectInput(display, x11Window, new IntPtr((int) XEventMask.ExposureMask));

XMapWindow(display, x11Window);
XFlush(display);

var gc = XCreateGC(display, x11Window, 0, 0);
var whitePixel = XWhitePixel(display, screen);
XSetForeground(display, gc, whitePixel);

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
    if (@event.type == XEventName.Expose)
    {
        XDrawLine(display, x11Window, gc, 10, 100, 100, 100);
    }
}