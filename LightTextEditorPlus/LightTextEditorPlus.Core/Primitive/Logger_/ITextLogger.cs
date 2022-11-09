using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
///     文本的日志记录
/// </summary>
public interface ITextLogger
{
    void LogDebug(string message);
    void LogException(Exception exception, string? message);
    void LogInfo(string message);
    void LogWarning(string message);
}