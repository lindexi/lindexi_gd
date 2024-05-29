using Microsoft.Maui.Graphics;

namespace UnoInk.Inking.X11Platforms.Input;

[ImplicitKeys(IsEnabled = false)]
public readonly record struct DeviceInputArgs(int Id, bool IsMouse, DeviceInputPoint Point)
{
    public Point Position => Point.Position;
    public ulong Timestamp => Point.Timestamp;

    public float? Pressure => Point.Pressure;

    public double? PhysicalWidth => Point.PhysicalWidth;
    public double? PhysicalHeight => Point.PhysicalHeight;

    public double? PixelWidth => Point.PixelWidth;
    public double? PixelHeight => Point.PixelHeight;

    /// <summary>
    /// 存放的触摸点数量
    /// </summary>
    /// <returns>可能为 -1 代表当前消息已过期</returns>
    public int DeviceInputPointCount
    {
        get
        {
            if (InputManager != null)
            {
                if (InputManager.MessageVersion != MessageVersion)
                {
                    return -1;
                }
                else
                {
                    return InputManager.CurrentCacheDeviceInputPointList.Count;
                }
            }
            else
            {
                return 1; // 只有自身 //[Point];
            }
        }
    }

    public IReadOnlyList<DeviceInputPoint> GetDeviceInputPoints()
    {
        if (InputManager != null)
        {
            if (InputManager.MessageVersion != MessageVersion)
            {
                throw new InvalidOperationException($"在当前消息结束之后，不能再获取触摸点。如需存储触摸点，请自行调用 ToList 方法");
            }
            else
            {
                return InputManager.CurrentCacheDeviceInputPointList;
            }
        }
        else
        {
            return [Point];
        }
    }

    internal uint MessageVersion { get; init; }
    internal X11DeviceInputManager? InputManager { get; init; }
}

public readonly record struct DeviceInputPoint(Point Position, ulong Timestamp)
{
    public float? Pressure { init; get; }

    public double? PhysicalWidth { init; get; }
    public double? PhysicalHeight { init; get; }

    public double? PixelWidth { init; get; }
    public double? PixelHeight { init; get; }
}
