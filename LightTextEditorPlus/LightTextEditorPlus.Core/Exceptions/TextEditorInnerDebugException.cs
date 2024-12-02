using System;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 文本框架内调试异常。如果你看到此异常，那就意味着文本框架内存在异常。快来将异常和步骤告诉文本开发者
/// 这个异常和 <see cref="TextEditorInnerException"/> 区别是，遇到这个异常时，文本框内部还能自行处理，只是在调试下抛出而已
/// </summary>
public class TextEditorInnerDebugException : TextEditorDebugException
{
    /// <inheritdoc/>
    public TextEditorInnerDebugException(string? message, object? exceptionData = null) : base(message, exceptionData)
    {
    }

    /// <inheritdoc/>
    public TextEditorInnerDebugException(string? message, Exception? innerException, object? exceptionData = null) : base(message, innerException, exceptionData)
    {
    }
}