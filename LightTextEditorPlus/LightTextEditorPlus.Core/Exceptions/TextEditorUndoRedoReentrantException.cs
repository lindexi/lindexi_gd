namespace LightTextEditorPlus.Core.Exceptions;

using LightTextEditorPlus.Core.Resources;

/// <summary>
/// 文本库撤销恢复重入异常
/// </summary>
public class TextEditorUndoRedoReentrantException : TextEditorException
{
    /// <inheritdoc />
    public override string Message => ExceptionMessages.Get(nameof(TextEditorUndoRedoReentrantException) + "_Message");
}
