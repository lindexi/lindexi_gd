using UnoInk.Inking.InkCore;
using UnoInk.Inking.InkCore.Interactives;

namespace DotNetCampus.Inking.Contexts;

/// <summary>
/// 动态笔迹层的上下文，一个点一个对象
/// </summary>
class DynamicStrokeContext
{
    public DynamicStrokeContext(InkingModeInputArgs lastInputArgs, AvaloniaSkiaInkCanvasSettings settings)
    {
        LastInputArgs = lastInputArgs;

        Stroke = new SkiaStroke(InkId.NewId())
        {
            Color = settings.InkColor,
            InkThickness = settings.InkThickness,
            IgnorePressure = settings.IgnorePressure,
        };
    }

    public InkingModeInputArgs LastInputArgs { get; }

    public int Id => LastInputArgs.Id;

    public SkiaStroke Stroke { get; }

    public override string ToString() => $"DynamicStrokeContext_{Id}";
}
