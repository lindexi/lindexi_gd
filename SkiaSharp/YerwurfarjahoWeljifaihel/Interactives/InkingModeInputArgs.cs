using BujeeberehemnaNurgacolarje;
using Microsoft.Maui.Graphics;

namespace SkiaInkCore.Interactives;

record InkingModeInputArgs(int Id, StylusPoint StylusPoint, ulong Timestamp)
{
    public Point Position => StylusPoint.Point;

    /// <summary>
    /// 是否来自鼠标的输入
    /// </summary>
    public bool IsMouse { init; get; }

    /// <summary>
    /// 被合并的其他历史的触摸点。可能为空
    /// </summary>
    public IReadOnlyList<StylusPoint>? StylusPointList { init; get; }
};