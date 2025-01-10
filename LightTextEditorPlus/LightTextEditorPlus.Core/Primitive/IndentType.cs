namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 表示缩进类型
/// </summary>
public enum IndentType
{
    /// <summary>
    ///悬挂缩进，指第一行包含度量值以及文本之前的距离(MarginLeft)的时候，
    ///该段第二行文本到边框的距离为度量值(Indent)与文本之前的距离(MarginLeft)的总和
    /// </summary>
    Hanging,

    /// <summary>
    ///首行缩进，只有第一行文本到边框的距离与文本之前的距离(MarginLeft)与
    ///度量值（Indent）有关，该段的第二行文本到边框的距离为文本之前的距离(MarginLeft)
    /// </summary>
    FirstLine,
}