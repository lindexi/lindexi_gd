using BujeeberehemnaNurgacolarje;

using KibairdaluBicikeenaiwaw;

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

var dictionary = new Dictionary<IntPtr, TestX11Window>();

var randr15ScreensImpl = new Randr15ScreensImpl(display, rootWindow);
var monitorInfos = randr15ScreensImpl.GetMonitorInfos();
for (var i = 0; i < monitorInfos.Length; i++)
{
    // 屏幕0 DisplayPort-1(343) IsPrimary=True XY=1920,309 WH=1920,1080
    // 屏幕1 DisplayPort-0(626) IsPrimary=False XY=0,0 WH=1920,1080
    MonitorInfo monitorInfo = monitorInfos[i];
    Console.WriteLine($"屏幕{i} {monitorInfo}");

    var x = monitorInfo.X;
    var y = monitorInfo.Y;

    var width = monitorInfo.Width;
    var height = monitorInfo.Height;

    var testX11Window = new TestX11Window($"Window{i}", x, y, width, height, display, rootWindow, screen, isFullScreen: true);

    testX11Window.MapWindow();

    testX11Window.SetFullScreenMonitor(i);
    await Task.Delay(TimeSpan.FromSeconds(1));
    testX11Window.SetFullScreen();

    testX11Window.Draw();

    dictionary[testX11Window.X11Window] = testX11Window;

    Console.WriteLine($"X11Window{i}={testX11Window.X11Window}");
}

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
        // ConfigureNotify XConfigureEvent (type=ConfigureNotify, serial=95, send_event=False, display=94855163599664, xevent=134217734, window=134217734, x=0, y=0, width=1920, height=1040, border_width=0, above=0, override_redirect=False)

        XConfigureEvent configureEvent = @event.ConfigureEvent;
        var window = configureEvent.window;
        if (dictionary.TryGetValue(window, out var x11Window))
        {
            Console.WriteLine($"ConfigureNotify '{x11Window.Name}' XY={configureEvent.x},{configureEvent.y} WH={configureEvent.width},{configureEvent.height}");
        }
    }
    else
    {
        //Console.WriteLine($"Event={@event}");
    }
}




