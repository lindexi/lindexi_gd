using System;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 调用文本库的撤销恢复专用方法时状态异常
/// </summary>
public class TextEditorNotInUndoRedoModeException : TextEditorException
{
    internal TextEditorNotInUndoRedoModeException(TextEditorCore textEditor) : base(textEditor)
    {
    }

    /// <inheritdoc />
    public override string ToString() =>
        ExceptionMessages.Format(nameof(TextEditorNotInUndoRedoModeException) + "_Message", TextEditor);
}