using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 行间距倍数，默认值为1，范围0~1000
/// </summary>
/// 行距的倍数需要根据 <see cref="LineSpacingAlgorithm"/> 进行决定
/// 另外是否加上行距计算，需要根据 <see cref="LineSpacingStrategy"/> 进行决定
public sealed record MultipleTextLineSpace(double LineSpacing) : ITextLineSpacing
{
}