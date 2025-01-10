namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 垂直文本对齐方式
/// </summary>
/// <remarks>
/// 这里的水平或垂直都是相对于横排文本的。<para/>
/// 对于横排文本，水平对齐是指文本的左右对齐方式，属于 X 方向的对齐方式，垂直对齐是指文本的上下对齐方式，属于 Y 方向的对齐方式。<para/>
/// 对于竖排文本，水平对齐属于 Y 方向的对齐方式，垂直对齐属于 X 方向的对齐方式。<para/>
/// </remarks>
public enum VerticalTextAlignment
{
    /// <summary>
    /// 顶部对齐
    /// </summary>
    Top,

    /// <summary>
    /// 居中对齐
    /// </summary>
    Center,

    /// <summary>
    /// 底部对齐
    /// </summary>
    Bottom,

    // 不能有 Stretch 因为不知道可以如何实现
    //Stretch,
}
