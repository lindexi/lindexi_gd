using System;
using System.Collections.Generic;
using System.Diagnostics;
using LightTextEditorPlus.Core.Diagnostics.LogInfos;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Layout.HitTests;

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

    public void Log<T>(T info) where T : notnull
    {
        // 不知道干啥咯
        if (!IsInDebugMode)
        {
            return;
        }

        if (info is StartLayoutLogInfo)
        {
            RecordMessage("===开始布局===");
        }
        else if (info is LayoutCompletedLogInfo layoutCompletedLogInfo)
        {
            foreach (LayoutDebugMessage message in layoutCompletedLogInfo.GetLayoutDebugMessageList())
            {
                // 不要输出了，因为附加调试时候，每一步都输出，太多了
                //LogDebug($"[Layout] {message}");
                RecordMessage($"[Layout]{message}", outputToDebug: false /*调试下不要在此输出。附加调试下，每一步都输出*/);
            }

            RecordMessage("===完成布局===");
        }
        else if (info is HitTestLogInfo hitTestLogInfo)
        {
            TextHitTestResult textHitTestResult = hitTestLogInfo.TextHitTestResult;

            RecordMessage
            (
                $"命中测试结果： HitPoint={hitTestLogInfo.HitPoint.ToMathPointFormat()}, 命中到第 {textHitTestResult.HitParagraphIndex.Index}段，第{textHitTestResult.LineLayoutData?.LineInParagraphIndex}行，字符为：'{textHitTestResult.HitCharData?.CharObject.ToText()}'。命中到空白={textHitTestResult.IsHitSpace}"
            );
        }
        else
        {
            RecordMessage(info.ToString() ?? string.Empty);
        }
    }

    private void RecordMessage(string message, bool outputToDebug = true)
    {
        if (outputToDebug)
        {
            Debug.WriteLine(message);
        }

        _logList ??= [];
        _logList.Add((DateTime.Now, message));
    }

    // 这个属性仅仅只是在调试下，给开发者看的，没有其他用途。因此就没有任何地方使用
    // ReSharper disable once CollectionNeverQueried.Local
    private List<(DateTime Time, string Message)>? _logList;
}