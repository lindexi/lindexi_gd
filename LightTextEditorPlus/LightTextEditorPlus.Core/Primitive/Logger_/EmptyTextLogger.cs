using System;
using System.Diagnostics;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 空白的文本日志
/// </summary>
internal class EmptyTextLogger : ITextLogger
{
    public EmptyTextLogger(TextEditorCore textEditor)
    {
        _textEditor = textEditor;
    }
    private readonly TextEditorCore _textEditor;

    public void LogDebug(string message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debugger.Log(0, "TextEditorCore", $"[Debug] {message}");
        }
    }

    public void LogException(Exception exception, string? message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debugger.Log(1, "TextEditorCore",$"[Warn] {message} 异常:{exception}");
        }
    }

    public void LogInfo(string message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debugger.Log(0, "TextEditorCore", $"[Info] {message}");
        }
    }

    public void LogWarning(string message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debugger.Log(1, "TextEditorCore", $"[Warn] {message}");
        }
    }
}