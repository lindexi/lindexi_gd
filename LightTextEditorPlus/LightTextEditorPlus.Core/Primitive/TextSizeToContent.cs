namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// Specifies how a window will automatically size itself to fit the size of its content.
/// </summary>
public enum TextSizeToContent
{
    /// <summary>
    /// 手动指定宽度和高度
    /// </summary>
    Manual = 0,

    /// <summary>
    /// 宽度自适应，高度手动
    /// </summary>
    Width = 1,

    /// <summary>
    /// 高度自适应，宽度手动
    /// </summary>
    Height = 2,

    /// <summary>
    /// 宽度高度自适应
    /// </summary>
    WidthAndHeight = 3,
}
