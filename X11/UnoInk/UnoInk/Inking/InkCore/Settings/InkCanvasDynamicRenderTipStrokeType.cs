namespace UnoInk.Inking.InkCore.Settings;

/// <summary>
/// 笔尖渲染模式
/// </summary>
public enum InkCanvasDynamicRenderTipStrokeType
{
    /// <summary>
    /// 通过裁剪画布的方式进行绘制所有的笔迹
    /// </summary>
    RenderAllTouchingStrokeWithClip,

    /// <summary>
    /// 所有触摸按下的笔迹都每次重新绘制，不区分笔尖和笔身
    /// 此方式可以实现比较好的平滑效果
    /// </summary>
    RenderAllTouchingStrokeWithoutTipStroke,

    /// <summary>
    /// 只渲染笔尖部分
    /// </summary>
    RenderTipStrokeOnly,
}