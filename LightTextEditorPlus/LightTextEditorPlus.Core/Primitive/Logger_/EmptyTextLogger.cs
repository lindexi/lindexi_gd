using System;
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

    public void LogDebug(string message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debug.WriteLine($"[Debug] {message}");
        }
    }

    public void LogException(Exception exception, string? message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debug.WriteLine($"[Warn] {message} 异常:{exception}");
        }
    }

    public void LogInfo(string message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debug.WriteLine($"[Info] {message}");
        }
    }

    public void LogWarning(string message)
    {
        if (_textEditor.IsInDebugMode)
        {
            Debug.WriteLine($"[Warn] {message}");
        }
    }

    public void Log<T>(T info)
    {
        // 不知道干啥咯
        if (!_textEditor.IsInDebugMode)
        {
            return;
        }

        if (info is LayoutCompletedLogInfo layoutCompletedLogInfo)
        {
            // 可选将其输出一下？
            // 调试下不要在此输出。附加调试下，每一步都输出
//#if DEBUG
//            // 只有在文本库调试模式下才会输出，不要影响业务开发者
//            foreach (string message in layoutCompletedLogInfo.GetLayoutDebugMessageList())
//            {
//                LogDebug($"[Layout] {message}");
//            }
//#endif
        }
    }
}
