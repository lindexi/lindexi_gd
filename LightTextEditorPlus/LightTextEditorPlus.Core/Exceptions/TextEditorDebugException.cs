using System;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 文本库调试异常，理论上不会在任何发布版本收到此异常。除非不小心开启了 <see cref="TextEditorCore.IsInDebugMode"/> 属性
/// </summary>
public class TextEditorDebugException : TextEditorException
{
    /// <inheritdoc cref="TextEditorDebugException"/>
    public TextEditorDebugException(string? message, object? exceptionData = null) : base(message)
    {
        ExceptionData = exceptionData;
    }

    /// <inheritdoc cref="TextEditorDebugException"/>
    public TextEditorDebugException(string? message, Exception? innerException, object? exceptionData = null) : base(
        message, innerException)
    {
        ExceptionData = exceptionData;
    }

    /// <summary>
    /// 异常信息
    /// </summary>
    public object? ExceptionData { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"调试异常，仅调试下抛出。{base.ToString()}\r\nTextEditor={TextEditor}";
    }
}