using System;
using LightTextEditorPlus.Core.Exceptions;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 文本布局过程中连续出现异常
/// </summary>
public class ForceLayoutContinuousException : TextEditorInnerException
{
    internal ForceLayoutContinuousException(Exception innerException) : base("文本布局过程中连续出现异常", innerException)
    {
    }
}