using System;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 文本的日志记录
/// </summary>
public interface ITextLogger
{
    void LogDebug(string message);
    void LogException(Exception exception, string? message);
    void LogInfo(string message);
    void LogWarning(string message);
}

class EmptyTextLogger : ITextLogger
{
    public void LogDebug(string message)
    {
        
    }

    public void LogException(Exception exception, string? message)
    {
    }

    public void LogInfo(string message)
    {
    }

    public void LogWarning(string message)
    {
    }
}