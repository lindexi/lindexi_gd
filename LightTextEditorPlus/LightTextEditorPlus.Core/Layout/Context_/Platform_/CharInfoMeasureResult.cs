using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 字符测量结果
/// </summary>
/// <param name="Bounds"></param>
public readonly record struct CharInfoMeasureResult(TextRect Bounds)
{
}