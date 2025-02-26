using System;
using System.Collections.Generic;
using System.Diagnostics;
using LightTextEditorPlus.Diagnostics.LogInfos;

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
    private bool IsInDebugMode => _textEditor.IsInDebugMode;

    public void LogDebug(string message)
    {
        if (IsInDebugMode)
        {
            RecordMessage($"[Debug] {message}");
        }
    }

    public void LogException(Exception exception, string? message)
    {
        if (IsInDebugMode)
        {
            RecordMessage($"[Warn] {message} 异常:{exception}");
        }
    }

    public void LogInfo(string message)
    {
        if (IsInDebugMode)
        {
            RecordMessage($"[Info] {message}");
        }
    }

    public void LogWarning(string message)
    {
        if (IsInDebugMode)
        {
            RecordMessage($"[Warn] {message}");
        }
    }

    public void Log<T>(T info)
    {
        // 不知道干啥咯
        if (!IsInDebugMode)
        {
            return;
        }

        if (info is LayoutCompletedLogInfo layoutCompletedLogInfo)
        {
            foreach (string message in layoutCompletedLogInfo.GetLayoutDebugMessageList())
            {
                LogDebug($"[Layout] {message}");
                RecordMessage($"[Layout] {message}", outputToDebug: false /*调试下不要在此输出。附加调试下，每一步都输出*/);
            }
        }
    }

    private void RecordMessage(string message, bool outputToDebug = true)
    {
        if (outputToDebug)
        {
            Debug.WriteLine(message);
        }

        _logList ??= new List<string>();
        _logList.Add(message);
    }

    private List<string>? _logList;
}