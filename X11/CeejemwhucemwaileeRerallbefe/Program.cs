using BujeeberehemnaNurgacolarje;

using CeejemwhucemwaileeRerallbefe;

using CPF.Linux;

using SkiaSharp;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static CPF.Linux.XLib;

XInitThreads();
var display = XOpenDisplay(IntPtr.Zero);
var screen = XDefaultScreen(display);
var rootWindow = XDefaultRootWindow(display);

Console.WriteLine("xxxxxxxxxxxxxxxxxxxxxxxx");

var randr15ScreensImpl = new Randr15ScreensImpl(display, rootWindow);
var monitorInfos = randr15ScreensImpl.GetMonitorInfos();
for (var i = 0; i < monitorInfos.Length; i++)
{
    Console.WriteLine($"屏幕{i} {monitorInfos[i]}");
}

var xDisplayWidth = XDisplayWidth(display, screen);
var xDisplayHeight = XDisplayHeight(display, screen);

Console.WriteLine($"XDisplayWidth={xDisplayWidth}");
Console.WriteLine($"XDisplayHeight={xDisplayHeight}");

var width = xDisplayWidth / 2;
var height = xDisplayHeight / 2;

var testX11Window1 = new TestX11Window(0, 0, width, height, display, rootWindow, screen);

testX11Window1.MapWindow();
testX11Window1.Draw();

Console.WriteLine($"X11Window1={testX11Window1.X11Window}");

var testX11Window2 = new TestX11Window(1920, 0, width, height, display, rootWindow, screen);
testX11Window2.MapWindow();
testX11Window2.Draw();

Console.WriteLine($"X11Window2={testX11Window2.X11Window}");

var dictionary = new Dictionary<IntPtr, TestX11Window>
{
    [testX11Window1.X11Window] = testX11Window1,
    [testX11Window2.X11Window] = testX11Window2
};


IntPtr invokeMessageId = new IntPtr(123123123);

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    if (@event.type == XEventName.Expose)
    {
        var window = @event.ExposeEvent.window;
        if (dictionary.TryGetValue(window, out var x11Window))
        {
            x11Window.Draw();
        }
    }
    else if (@event.type == XEventName.ClientMessage)
    {
        if (@event.ClientMessageEvent.ptr1 == invokeMessageId)
        {

        }
    }
    else if (@event.type == XEventName.PropertyNotify)
    {
        var atom = @event.PropertyEvent.atom;
        var atomNamePtr = XGetAtomName(display, atom);
        var atomName = Marshal.PtrToStringAnsi(atomNamePtr);
        XFree(atomNamePtr);
        //Console.WriteLine($"PropertyNotify {atomName}({atom}) State={@event.PropertyEvent.state}");
    }
    else if (@event.type is XEventName.KeymapNotify)
    {
        // 忽略
    }
    else if (@event.type is XEventName.ConfigureNotify)
    {
        Console.WriteLine($"ConfigureNotify {@event}");
    }
    else
    {
        //Console.WriteLine($"Event={@event}");
    }
}




