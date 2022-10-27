using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 空段落的行高测量
/// </summary>
public interface IEmptyParagraphLineHeightMeasurer
{
    EmptyParagraphLineHeightMeasureResult MeasureEmptyParagraphLineHeight(in EmptyParagraphLineHeightMeasureArgument argument);
}