using System;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 在文本是脏的获取了渲染信息
/// </summary>
public class TextEditorRenderInfoDirtyException : TextEditorException
{
    internal TextEditorRenderInfoDirtyException(TextEditorCore textEditor) : base(textEditor)
    {
    }

    /// <inheritdoc />
    public override string Message =>
        ExceptionMessages.Format(nameof(TextEditorRenderInfoDirtyException) + "_Message",
            TextEditor?.GetLayoutUpdateReason(), TextEditor);
}
