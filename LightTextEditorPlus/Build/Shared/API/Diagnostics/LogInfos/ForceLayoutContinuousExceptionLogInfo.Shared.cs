using System;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 文本布局过程中连续出现异常
/// </summary>
public readonly record struct ForceLayoutContinuousExceptionLogInfo(Exception CurrentException);