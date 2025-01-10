using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="LineSize">这一行的尺寸</param>
/// <param name="TextSize">这一行的文本字符的尺寸</param>
/// <param name="CharCount">这一行使用的 字符 的数量</param>
public readonly record struct WholeLineLayoutResult(TextSize LineSize, TextSize TextSize, int CharCount)
{
    // todo 这一行的字符起点信息，相对于本行。感觉不如行范围
}
