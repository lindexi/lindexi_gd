using BujeeberehemnaNurgacolarje;

namespace SkiaInkCore.Interactives;

static class ModeInputArgsExtension
{
    public static InkingModeInputArgs ToModeInputArgs(this DeviceInputArgs args, bool ignorePressure = true)
    {
        var deviceInputPoint = args.Point;

        IReadOnlyList<StylusPoint>? stylusPointList;
        var count = args.DeviceInputPointCount;
        if (count < 1)
        {
            stylusPointList = null;
        }
        else if (count == 1)
        {
            stylusPointList = [ToStylusPoint(in deviceInputPoint, ignorePressure)];
        }
        else
        {
            stylusPointList = args.GetDeviceInputPoints().Select(t => ToStylusPoint(in t, ignorePressure)).ToList();
        }

        return new InkingModeInputArgs(args.Id, ToStylusPoint(in deviceInputPoint, ignorePressure), args.Timestamp)
        {
            IsMouse = args.IsMouse,
            StylusPointList = stylusPointList,
        };
    }

    public static StylusPoint ToStylusPoint(in DeviceInputPoint point, bool ignorePressure = true) =>
        new StylusPoint(point.Position.ToFoundationPoint(), !ignorePressure ? point.Pressure ?? 0.5f : 0.5f)
        {
            IsPressureEnable = !ignorePressure && point.Pressure is not null,
            Width = point.PixelWidth,
            Height = point.PixelHeight
        };
}