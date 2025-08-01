using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 命中测试的日志记录信息
/// </summary>
/// <param name="HitPoint"></param>
/// <param name="TextHitTestResult"></param>
/// <param name="LogContext"></param>
public readonly record struct HitTestLogInfo(TextPoint HitPoint, TextHitTestResult TextHitTestResult, TextEditorDebugLogContext LogContext);