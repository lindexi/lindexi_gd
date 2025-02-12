using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="LineSize">这一行的尺寸</param>
/// <param name="TextSize">这一行的文本字符的尺寸</param>
/// <param name="CharCount">这一行使用的 字符 的数量</param>
/// <param name="LineSpacingThickness"></param>
public readonly record struct WholeLineLayoutResult(TextSize LineSize, TextSize TextSize, int CharCount, TextThickness LineSpacingThickness)
{
}
