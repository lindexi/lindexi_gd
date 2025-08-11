using BujeeberehemnaNurgacolarje;

using CPF.Linux;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using WhonurqaikarjurceLallchelceeqalbear;

using static CPF.Linux.XLib;

XInitThreads();

Console.WriteLine("XInitThreads");

var display = XOpenDisplay(IntPtr.Zero);
Console.WriteLine("XOpenDisplay");
var screen = XDefaultScreen(display);
Console.WriteLine("XDefaultScreen");
var rootWindow = XDefaultRootWindow(display);
Console.WriteLine("XDefaultRootWindow");

var randr15ScreensImpl = new Randr15ScreensImpl(display, rootWindow);
MonitorInfo[] monitorInfos = randr15ScreensImpl.GetMonitorInfos();

for (var i = 0; i < monitorInfos.Length; i++)
{
    MonitorInfo monitorInfo = monitorInfos[i];
    Console.WriteLine(monitorInfo);

    OutputEdidInfo(monitorInfo);
}

unsafe void OutputEdidInfo(MonitorInfo monitorInfo)
{
    var edidAtom = XInternAtom(display, "EDID", only_if_exists: true);
    var anyPropertyTypeAtom = XInternAtom(display, "AnyPropertyType", only_if_exists: true);
    const nint XA_INTEGER = 19; //XInternAtom(display, "XA_INTEGER", only_if_exists: true);

    for (var i = 0; i < monitorInfo.Outputs.Length; i++)
    {
        var rrOutput = monitorInfo.Outputs[i];
        if (rrOutput == IntPtr.Zero)
        {
            continue;
        }

        var properties = XRRListOutputProperties(display, rrOutput, out var propertyCount);
        IntPtr prop = 0;

        try
        {
            var hasEDID = false;
            for (var pc = 0; pc < propertyCount; pc++)
            {
                if (properties[pc] == edidAtom)
                {
                    hasEDID = true;
                    break;
                }
            }

            if (!hasEDID)
            {
                Console.WriteLine($"Output {rrOutput} does not have EDID property.");
                continue;
            }

            // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
            const int EDIDStructureLength = 32;

            XRRGetOutputProperty(display, rrOutput, edidAtom, 0, EDIDStructureLength, false, false,
                anyPropertyTypeAtom, out IntPtr actualType, out int actualFormat, out int nItems, out long bytesAfter,
                out prop);

            // https://gitlab.gnome.org/GNOME/mutter/-/blame/3.29.90/src/backends/x11/meta-output-xrandr.c#L193
            // exists = (actual_type == XA_INTEGER && actual_format == 32 && nitems == 1);

            Console.WriteLine($"actualType={actualType} nItems={nItems} bytesAfter={bytesAfter}");

            if (actualType != XA_INTEGER)
            {
                continue;
            }

            if (actualFormat != 8) // Expecting a byte array
            {
                continue;
            }

            Span<byte> edid = new Span<byte>((void*) prop, (int) bytesAfter);
            //Marshal.Copy(prop, edid, 0, (int) bytesAfter);

            ReadEdidInfoResult edidInfoResult = EdidInfo.ReadEdid(edid);
            if (edidInfoResult.IsSuccess)
            {
                EdidInfo edidInfo = edidInfoResult.EdidInfo;
                Console.WriteLine($"EDID Info: ManufacturerName={edidInfo.ManufacturerName} MonitorPhysical={edidInfo.BasicDisplayParameters.MonitorPhysicalWidth.Value}x{edidInfo.BasicDisplayParameters.MonitorPhysicalHeight.Value}cm");
            }
            else
            {
                Console.WriteLine($"解析 Edid 失败 {edidInfoResult.ErrorMessage}");
            }
        }
        finally
        {
            XLib.XFree(prop);
            XLib.XFree(new IntPtr(properties));
        }
    }
}

IntPtr invokeMessageId = new IntPtr(123123123);

while (true)
{
    var xNextEvent = XNextEvent(display, out var @event);

    if (xNextEvent != 0)
    {
        break;
    }

    Console.WriteLine($"Event={@event}");

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
            Console.WriteLine($"收到消息");
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
    }
}




