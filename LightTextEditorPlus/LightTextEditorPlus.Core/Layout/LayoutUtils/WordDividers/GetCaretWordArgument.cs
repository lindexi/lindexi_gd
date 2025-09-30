using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

/// <summary>
/// 获取传入光标所在的单词选择范围的参数
/// </summary>
/// <param name="CaretOffset"></param>
/// <param name="TextEditor"></param>
public readonly record struct GetCaretWordArgument(CaretOffset CaretOffset,TextEditorCore TextEditor);

/// <summary>
/// 获取传入光标所在的单词选择范围的结果
/// </summary>
public readonly record struct GetCaretWordResult()
{
    /// <summary>
    /// 单词范围
    /// </summary>
    public required Selection WordSelection { get; init; }

    /// <summary>
    /// 是否命中到标点符号。空格也算标点符号
    /// </summary>
    public bool HitPunctuation { get; init; } = false;
}