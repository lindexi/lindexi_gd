namespace UnoInk.Inking.InkCore.Settings;

/// <summary>
/// 对清空笔迹的配置
/// </summary>
public record CleanStrokeSettings
{
    /// <summary>
    /// 清空笔迹之后，需要绘制背景图。对于一些背景是没有任何内容的应用，则不需要绘制，提升性能。因为清空笔迹之后，会将当前的静态笔迹都绘制一次，除非背景有图片或其他内容，否则不需要绘制背景
    /// </summary>
    public bool ShouldDrawBackground { get; init; } = false;

    /// <summary>
    /// 清空笔迹之后，是否需要更新背景图。解决当前有两个笔迹，只删除其中一个笔迹，如果此时背景没有更新，则可能导致两个笔迹都被删除，或被删除的笔迹依然在背景里
    /// </summary>
    public bool ShouldUpdateBackground { get; init; } = true;
}
