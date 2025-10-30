using DotNetCampus.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using WhonurqaikarjurceLallchelceeqalbear;
using static X11ApplicationFramework.Natives.XLib;

using X11ApplicationFramework.Apps;

namespace X11ApplicationFramework.Natives;
// Copy from https://github.com/AvaloniaUI/Avalonia \src\Avalonia.X11\Screens\X11Screen.Providers.cs

/// <summary>
/// 用于获取多屏的信息
/// </summary>
[SupportedOSPlatform("Linux")]
class Randr15ScreensImpl
{
    public static Randr15ScreensImpl? TryCreateXRandr15ScreensImpl(X11Application application)
    {
        var x11Info = application.X11Info;
        var display = x11Info.Display;

        try
        {
            if (XRRQueryExtension(display, out int randrEventBase, out var randrErrorBase) != 0)
            {
                if (XRRQueryVersion(display, out var major, out var minor) != 0)
                {
                    var randrVersion = new Version(major, minor);

                    if (randrVersion >= new Version(1, 5))
                    {
                        return new Randr15ScreensImpl(application)
                        {
                            RandrErrorBase = randrErrorBase,
                            RandrEventBase = randrEventBase,
                            RandrVersion = randrVersion
                        };
                    }
                }
            }
        }
        catch
        {
            // Ignore, randr is not supported
            // 无法支持，忽略
        }

        return null;
    }

    private Randr15ScreensImpl(X11Application application)
    {
        var x11Info = application.X11Info;
        var display = x11Info.Display;
        var rootWindow = x11Info.RootWindow;

        _display = display;
        var eventWindow = CreateEventWindow(display, rootWindow);

        var xRandrWindow = new XRandrWindow(application, eventWindow, this);

        _window = xRandrWindow;

        XRRSelectInput(display, eventWindow, RandrEventMask.RRScreenChangeNotify);
    }

    public required Version RandrVersion { get; init; }

    public required int RandrErrorBase { get; init; }

    public required int RandrEventBase { get; init; }

    public event EventHandler? Changed;

    private MonitorInfo[]? _cache;

    public MonitorInfo[] GetMonitorInfos()
    {
        return _cache ??= GetMonitorInfos(_display, _window.X11WindowIntPtr);
    }

    public static unsafe MonitorInfo[] GetMonitorInfos(nint display, nint window)
    {
        XRRMonitorInfo* monitors = XRRGetMonitors(display, window, true, out var count);
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
                Index = c,
                IsPrimary = mon.Primary != 0,
                X = mon.X,
                Y = mon.Y,
                Width = mon.Width,
                Height = mon.Height,
                Outputs = outputs,
                Display = display,
            };
        }

        XFree(new IntPtr(monitors));

        return screens;
    }

    private readonly IntPtr _display;

    public X11Window EventWindow => _window;
    private readonly XRandrWindow _window;

    private class XRandrWindow : X11Window
    {
        public XRandrWindow(X11Application application, IntPtr x11WindowIntPtr, Randr15ScreensImpl randr15ScreensImpl) : base(application, x11WindowIntPtr)
        {
            _randr15ScreensImpl = randr15ScreensImpl;
        }

        private readonly Randr15ScreensImpl _randr15ScreensImpl;

        protected override unsafe void OnDispatchEvent(XEvent* @event)
        {
            if ((int) @event->type == _randr15ScreensImpl.RandrEventBase + (int) RandrEvent.RRScreenChangeNotify)
            {
                Log.Debug($"[XRandrWindow] RRScreenChangeNotify");
                _randr15ScreensImpl._cache = null;
                _randr15ScreensImpl.Changed?.Invoke(_randr15ScreensImpl, EventArgs.Empty);
                return;
            }

            base.OnDispatchEvent(@event);
        }
    }

    public MonitorInfo? GetMonitorInfo(string monitorId)
    {
        return GetMonitorInfos().FirstOrDefault(monitor => monitor.GetNameText() == monitorId);
    }
}

file enum RandrEvent
{
    RRScreenChangeNotify = 0,

    /* V1.2 additions */
    RRNotify = 1
}

public readonly record struct MonitorInfo
{
    public IntPtr Name { get; init; }
    public bool IsPrimary { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public IntPtr[] Outputs { get; init; }
    public IntPtr Display { get; init; }
    /// <summary>
    /// 显示器序号
    /// </summary>
    public int Index { get; init; }

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

    /// <summary>
    /// 尝试读取当前屏幕的 EDID 信息
    /// </summary>
    /// [dotnet X11 获取多屏 edid 信息 - lindexi - 博客园](https://www.cnblogs.com/lindexi/p/19033056 )
    /// <returns></returns>
    public unsafe EdidInfo? TryGetEdidInfo()
    {
        var display = Display;
        var edidAtom = XInternAtom(display, "EDID", only_if_exists: true);

        var anyPropertyTypeAtom = XInternAtom(display, "AnyPropertyType", only_if_exists: true);
        const nint XA_INTEGER = 19;

        for (var i = 0; i < Outputs.Length; i++)
        {
            var rrOutput = Outputs[i];
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
                    Log.Debug($"Output {rrOutput} does not have EDID property.");
                    continue;
                }

                // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
                const int EDIDStructureLength = 32;

                XRRGetOutputProperty(display, rrOutput, edidAtom, 0, EDIDStructureLength, false, false,
                    anyPropertyTypeAtom, out IntPtr actualType, out int actualFormat, out int nItems,
                    out long bytesAfter,
                    out prop);

                if (actualType != XA_INTEGER)
                {
                    continue;
                }

                if (actualFormat != 8) // Expecting a byte array
                {
                    continue;
                }

                Span<byte> edid = new Span<byte>((void*) prop, (int) bytesAfter);
                ReadEdidInfoResult edidInfoResult = EdidInfo.ReadEdid(edid);

                if (edidInfoResult.IsSuccess)
                {
                    EdidInfo edidInfo = edidInfoResult.EdidInfo;
                    Log.Info(
                        $"EDID Info: ManufacturerName={edidInfo.ManufacturerName} MonitorPhysical={edidInfo.BasicDisplayParameters.MonitorPhysicalWidth.Value}x{edidInfo.BasicDisplayParameters.MonitorPhysicalHeight.Value}cm");
                    return edidInfo;
                }
                else
                {
                    Log.Warn($"解析 Edid 失败 {edidInfoResult.ErrorMessage}");
                }
            }
            finally
            {
                XLib.XFree(prop);
                XLib.XFree(new IntPtr(properties));
            }
        }

        return null;
    }
}