using CPF.Linux;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static CPF.Linux.XLib;

namespace BujeeberehemnaNurgacolarje;

// Copy from https://github.com/AvaloniaUI/Avalonia \src\Avalonia.X11\Screens\X11Screen.Providers.cs

public class Randr15ScreensImpl
{
    public Randr15ScreensImpl(nint display, nint rootWindow)
    {
        _display = display;
        var eventWindow = CreateEventWindow(display, rootWindow);
        _window = eventWindow;

        XRRSelectInput(display, _window, RandrEventMask.RRScreenChangeNotify);
    }

    public unsafe MonitorInfo[] GetMonitorInfos()
    {
        XRRMonitorInfo* monitors = XRRGetMonitors(_display, _window, true, out var count);
        var screens = new MonitorInfo[count];
        for (var c = 0; c < count; c++)
        {
            var mon = monitors[c];

            var outputs = new nint[mon.NOutput];

            for (int i = 0; i < outputs.Length; i++)
            {
                outputs[i] = mon.Outputs[i];
            }

            screens[c] = new MonitorInfo()
            {
                Name = mon.Name,
                IsPrimary = mon.Primary != 0,
                X = mon.X,
                Y = mon.Y,
                Width = mon.Width,
                Height = mon.Height,
                Outputs = outputs,
                Display = _display,
            };
        }

        return screens;
    }

    private readonly IntPtr _display;

    private readonly IntPtr _window;
}

public unsafe struct MonitorInfo
{
    public IntPtr Name;
    public bool IsPrimary;
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public IntPtr[] Outputs;
    public IntPtr Display { get; init; }

    public string? GetNameText()
    {
        var namePtr = XGetAtomName(Display, Name);
        var name = Marshal.PtrToStringAnsi(namePtr);
        XFree(namePtr);
        return name;
    }

    public override string ToString()
    {
        var name = GetNameText();

        return $"{name}({Name}) IsPrimary={IsPrimary} XY={X},{Y} WH={Width},{Height}";
    }
}