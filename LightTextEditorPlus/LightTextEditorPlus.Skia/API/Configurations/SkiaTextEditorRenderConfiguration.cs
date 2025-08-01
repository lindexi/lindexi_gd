namespace LightTextEditorPlus.Configurations;

/// <summary>
/// 渲染配置
/// </summary>
public readonly record struct SkiaTextEditorRenderConfiguration()
{
    /// <summary>
    /// 使用逐字渲染方法。渲染效率慢，但可以遵循布局结果
    /// </summary>
    /// 如需要实现类似控制台文本控制强行等宽效果，则需要开启此选项
    public bool UseRenderCharByCharMode { get; init; } = false;

    /// <summary>
    /// 字墨在字外框内的对齐方式
    /// </summary>
    public SkiaTextEditorCharRenderFaceInFrameAlignment RenderFaceInFrameAlignment { get; init; } =
        SkiaTextEditorCharRenderFaceInFrameAlignment.Left;
}

/// <summary>
/// 字墨在字外框内的对齐方式
/// </summary>
/// <remarks>仅横排生效，仅只在逐字渲染时才能生效</remarks>
public enum SkiaTextEditorCharRenderFaceInFrameAlignment : byte
{
    /// <summary>
    /// 左对齐
    /// </summary>
    Left = 0,

    /// <summary>
    /// 居中对齐
    /// </summary>
    Center,

    /// <summary>
    /// 右对齐
    /// </summary>
    Right,
}