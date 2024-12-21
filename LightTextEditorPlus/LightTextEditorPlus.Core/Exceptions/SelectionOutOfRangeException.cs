using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 选择范围超过文档异常
/// </summary>
public class SelectionOutOfRangeException : TextEditorException
{
    /// <summary>
    /// 选择范围超过文档异常
    /// </summary>
    public SelectionOutOfRangeException(TextEditorCore textEditor, in Selection selection, int documentCharCount)
    {
        Selection = selection;
        DocumentCharCount = documentCharCount;
        TextEditor = textEditor;
    }

    /// <summary>
    /// 选择的内容
    /// </summary>
    public Selection Selection { get; }

    /// <summary>
    /// 文档字符数量
    /// </summary>
    public int DocumentCharCount { get; }

    /// <inheritdoc />
    public override string Message =>
        $"Selection from {Selection.FrontOffset.Offset} to {Selection.BehindOffset.Offset} Length={Selection.Length} DocumentCharCount={DocumentCharCount};TextEditor={TextEditor}";
}