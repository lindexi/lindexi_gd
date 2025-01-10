namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本水平对齐方式
/// </summary>
/// <remarks>
/// 这里的水平或垂直都是相对于横排文本的。<para/>
/// 对于横排文本，水平对齐是指文本的左右对齐方式，属于 X 方向的对齐方式，垂直对齐是指文本的上下对齐方式，属于 Y 方向的对齐方式。<para/>
/// 对于竖排文本，水平对齐属于 Y 方向的对齐方式，垂直对齐属于 X 方向的对齐方式。<para/>
/// </remarks>
public enum HorizontalTextAlignment
{
    /// <summary>
    /// 左对齐
    /// </summary>
    Left,

    /// <summary>
    /// 右对齐
    /// </summary>
    Right,

    /// <summary>
    /// 居中对齐
    /// </summary>
    Center,

    /// <summary>
    /// 两端对齐
    /// </summary>
    Justify,

    /// <summary>
    /// 分散对齐
    /// </summary>
    Distribute,
}
