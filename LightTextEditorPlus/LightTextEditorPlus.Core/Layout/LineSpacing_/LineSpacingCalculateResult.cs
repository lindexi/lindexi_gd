namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 行距计算结果
/// </summary>
/// <param name="ShouldUseCharLineHeight">是否应该使用字符的行高</param>
/// <param name="TotalLineHeight">总行高</param>
public readonly record struct LineSpacingCalculateResult(bool ShouldUseCharLineHeight, double TotalLineHeight);