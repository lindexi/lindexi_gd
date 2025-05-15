using System;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

public readonly record struct TextEditorUpdateLayoutExceptionLogInfo(Exception UpdateLayoutException);