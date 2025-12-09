using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using X11ApplicationFramework.Apps;
using X11ApplicationFramework.Natives;
using X11ApplicationFramework.Utils.Edid;

using static X11ApplicationFramework.Natives.XLib;

namespace X11ApplicationFramework.Utils;

public static unsafe class MultiScreensDisplayInfoHelper
{
    public static List<DisplayInfo> GetDisplayInfos(X11InfoManager x11Info)
    {
        XRRMonitorInfo* monitors = XRRGetMonitors(x11Info.Display, x11Info.RootWindow, true, out var count);
        List<DisplayInfo> displayInfos = new List<DisplayInfo>(count);

        for (var i = 0; i < count; i++)
        {
            var monitor = monitors[i];
            var outputs = monitor.Outputs;
            var outputCount = monitor.NOutput;
            string? name = null;

            EdidInfo? edid = null;

            for (var j = 0; j < outputCount; j++)
            {
                var output = outputs[j];

                if (TryGetEdidInfo(x11Info, output, out var edidInfo))
                {
                    edid = edidInfo;
                    var nameFromEdid = edidInfo.ManufacturerName;
                    if (!string.IsNullOrEmpty(nameFromEdid))
                    {
                        name = nameFromEdid;
                        break;
                    }
                }
            }

            displayInfos.Add(new DisplayInfo
            {
                Width = monitor.Width,
                Height = monitor.Height,
                Left = monitor.X,
                Top = monitor.Y,
                EDIDName = name,
                EdidInfo = edid,
                IsPrimary = monitor.Primary != 0
            });
        }

        return displayInfos;
    }

    // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
    private const int EDIDStructureLength = 32;

    private static bool TryGetEdidInfo(X11InfoManager x11, IntPtr rrOutput, out EdidInfo edidInfo)
    {
        edidInfo = default;
        if (rrOutput == IntPtr.Zero)
        {
            return false;
        }

        var properties = XRRListOutputProperties(x11.Display, rrOutput, out var propertyCount);
        IntPtr prop = 0;

        try
        {
            var hasEDID = false;
            for (var pc = 0; pc < propertyCount; pc++)
            {
                if (properties[pc] == x11.X11Atoms.EDID)
                    hasEDID = true;
            }

            if (!hasEDID)
            {
                return false;
            }

            XRRGetOutputProperty(x11.Display, rrOutput, x11.X11Atoms.EDID, 0, EDIDStructureLength, false, false,
                x11.X11Atoms.AnyPropertyType, out IntPtr actualType, out int actualFormat, out _, out var bytesAfter,
                out prop);
            if (actualType != x11.X11Atoms.XA_INTEGER)
            {
                return false;
            }

            if (actualFormat != 8) // Expecting a byte array
            {
                return false;
            }

            Span<byte> edid = new Span<byte>((void*) prop, (int) bytesAfter);
            ReadEdidInfoResult edidInfoResult = EdidInfo.ReadEdid(edid);
            edidInfo = edidInfoResult.EdidInfo;

            return edidInfoResult.IsSuccess;
        }
        finally
        {
            XLib.XFree(prop);
            XLib.XFree(new IntPtr(properties));
        }
    }
}

public record DisplayInfo
{
    public int Width { get; init; }

    public int Height { get; init; }

    public int Left { get; init; }

    public int Top { get; init; }

    public string? EDIDName { get; init; }

    public EdidInfo? EdidInfo { get; init; }

    public bool IsPrimary { get; init; }
}