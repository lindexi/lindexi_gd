namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 文本库撤销恢复重入异常
/// </summary>
public class TextEditorUndoRedoReentrantException : TextEditorException
{
    /// <inheritdoc />
    public override string Message => "在文本库撤销恢复过程中，产生了新的撤销恢复";
}
