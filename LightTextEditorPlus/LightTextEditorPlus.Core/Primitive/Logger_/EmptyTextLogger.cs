using System;

namespace LightTextEditorPlus.Core.Primitive;

internal class EmptyTextLogger : ITextLogger
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