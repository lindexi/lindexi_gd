using CPF.Linux;

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime;

using static CPF.Linux.XLib;

var display = XOpenDisplay(IntPtr.Zero);
var rootWindow = XDefaultRootWindow(display);

var x11Window =
XCreateSimpleWindow(display, rootWindow, 100, 100, 500, 500, 1, IntPtr.Zero, IntPtr.Zero);

XMapWindow(display, x11Window);
XFlush(display);

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);
}