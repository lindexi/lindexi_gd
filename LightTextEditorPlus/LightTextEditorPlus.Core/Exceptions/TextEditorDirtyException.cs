using System;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 表示当前的文本被更改，还没有完成布局渲染，不能获取渲染布局相关内容
/// </summary>
public class TextEditorDirtyException : TextEditorException
{
    internal TextEditorDirtyException(TextEditorCore textEditor) : base(textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditorCore _textEditor;

    /// <inheritdoc />
    public override string Message
    {
        get
        {
            string message = ExceptionMessages.Get(nameof(TextEditorDirtyException) + "_Message");
            if (_textEditor.IsFinishUpdateLayoutWithException)
            {
                message += ExceptionMessages.Format(
                    nameof(TextEditorDirtyException) + "_FinishUpdateLayoutWithExceptionSuffix",
                    nameof(TextEditorCore.IsFinishUpdateLayoutWithException));
            }
            return message;
        }
    }
}