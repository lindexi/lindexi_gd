using System;

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
            string message = "当前的文本被更改，还没有完成布局渲染，不能获取渲染布局相关内容。";
            if (_textEditor.IsFinishUpdateLayoutWithException)
            {
                message += $"由于文本上次布局过程存在异常，则可能文本已经确实经过布局了，但被异常打断，不能完成布局。TextEditor.{nameof(TextEditorCore.IsFinishUpdateLayoutWithException)}=True";
            }
            return message;
        }
    }
}