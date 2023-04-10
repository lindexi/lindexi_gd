using System;

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
    public override string ToString() => $"调用文本库的撤销恢复专用方法时，文本库没有进入撤销恢复模式。TextEditor={TextEditor}";
}