using System;
using System.Runtime.Serialization;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 文本库的异常
/// </summary>
public abstract class TextEditorException : Exception
{
    /// <inheritdoc cref="TextEditorException"/>
    protected TextEditorException()
    {
    }

    /// <inheritdoc cref="TextEditorException"/>
    protected TextEditorException(string? message) : base(message)
    {
    }

    /// <inheritdoc cref="TextEditorException"/>
    protected TextEditorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <inheritdoc cref="TextEditorException"/>
    protected TextEditorException(TextEditorCore textEditor)
    {
        TextEditor = textEditor;
    }

    /// <summary>
    /// 抛出异常的文本
    /// </summary>
    public TextEditorCore? TextEditor { init; get; }

    /// <inheritdoc />
    public override string ToString()
    {
        if (TextEditor is null)
        {
            return base.ToString();
        }

        return $"TextEditor={TextEditor}\r\n{base.ToString()}";
    }
}
