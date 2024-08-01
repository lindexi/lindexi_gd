namespace SkiaInkCore.Settings;

/// <summary>
/// 笔尖渲染模式
/// </summary>
enum InkCanvasDynamicRenderTipStrokeType
{
    /// <summary>
    /// 通过裁剪画布的方式进行绘制所有的笔迹
    /// </summary>
    /// 这是当前最快的笔迹，写的快炸的快
    /// 这里面用了绕过 Skia 的裁剪，使用 <see cref="SkiaExtension.ReplacePixels(SkiaSharp.SKBitmap,SkiaSharp.SKBitmap)"/> 替换为背景
    RenderAllTouchingStrokeWithClip,

    /// <summary>
    /// 所有触摸按下的笔迹都每次重新绘制，不区分笔尖和笔身
    /// 此方式可以实现比较好的平滑效果
    /// </summary>
    /// 此方式性能比较差，但是最符合预期的
    RenderAllTouchingStrokeWithoutTipStroke,

    /// <summary>
    /// 只渲染笔尖部分
    /// </summary>
    RenderTipStrokeOnly,
}