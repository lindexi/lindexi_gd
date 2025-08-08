using BujeeberehemnaNurgacolarje;

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

IntPtr invokeMessageId = new IntPtr(123123123);

//Task.Run(() =>
//{
//    var newDisplay = XOpenDisplay(IntPtr.Zero);

//    while (true)
//    {
//        Console.ReadLine();

//        var handle = dictionary.First().Value.X11Window;

//        var xEvent = new XEvent
//        {
//            ClientMessageEvent = 
//            {
//                type = XEventName.ClientMessage,
//                send_event = true,
//                window = handle,
//                display = newDisplay,
//                message_type = 0,
//                format = 32,
//                ptr1 = invokeMessageId
//            }
//        };
//        // [Xlib Programming Manual: Expose Events](https://tronche.com/gui/x/xlib/events/exposure/expose.html )
//        XLib.XSendEvent(newDisplay, handle, propagate: false,
//            0,
//            ref xEvent);

//        XFlush(newDisplay);
//    }

//    XCloseDisplay(newDisplay);
//});

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
        //if (dictionary.TryGetValue(window, out var x11Window))
        //{
        //    x11Window.Draw();
        //}
    }
    else if (@event.type == XEventName.ClientMessage)
    {
        if (@event.ClientMessageEvent.ptr1 == invokeMessageId)
        {
            Console.WriteLine($"设置全屏");
            //foreach (var testX11Window in dictionary.Values)
            //{
            //    testX11Window.SetFullScreen();
            //}
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
    }
    else
    {
        //Console.WriteLine($"Event={@event}");
    }
}




