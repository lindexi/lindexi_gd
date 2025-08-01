using System;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 更新布局过程中出现异常的日志信息
/// </summary>
/// <param name="UpdateLayoutException"></param>
public readonly record struct TextEditorUpdateLayoutExceptionLogInfo(Exception UpdateLayoutException);