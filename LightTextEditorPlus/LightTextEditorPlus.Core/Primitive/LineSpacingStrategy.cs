namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 多倍行距呈现策略
/// </summary>
public enum LineSpacingStrategy
{
    /// <summary>
    /// 空间全部展开，例如ppt的文本框
    /// </summary>
    FullExpand,

    /// <summary>
    /// 首段首行空间不展开，例如 WPF 的文本框
    /// </summary>
    FirstLineShrink
}