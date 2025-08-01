using LightTextEditorPlus.Core.Editing;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 文本特性被禁用的日志信息
/// </summary>
/// <param name="Features"></param>
public readonly record struct TextFeaturesBeDisabledLogInfo(TextFeatures Features);