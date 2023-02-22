using System;
using System.Runtime.Serialization;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 文本库的异常
/// </summary>
public abstract class TextEditorException : Exception
{
    protected TextEditorException()
    {
    }

    protected TextEditorException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    protected TextEditorException(string? message) : base(message)
    {
    }

    protected TextEditorException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}