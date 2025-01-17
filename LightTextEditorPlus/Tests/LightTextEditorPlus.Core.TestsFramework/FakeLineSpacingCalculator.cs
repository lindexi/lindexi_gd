using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.TestsFramework;

/// <summary>
/// 让行高和字号一样，方便测试
/// </summary>
public class FakeLineSpacingCalculator : ILineSpacingCalculator
{
    public LineSpacingCalculateResult CalculateLineSpacing(in LineSpacingCalculateArgument argument)
    {
        ITextLineSpacing textLineSpacing = argument.ParagraphProperty.LineSpacing;
        if (textLineSpacing is MultipleTextLineSpace multipleTextLineSpace)
        {
            return new LineSpacingCalculateResult(false,
                argument.MaxFontSizeCharRunProperty.FontSize * multipleTextLineSpace.LineSpacing, 0);
        }
        else
        {
            ExactlyTextLineSpace exactlyTextLineSpace = (ExactlyTextLineSpace) textLineSpacing;
            return new LineSpacingCalculateResult(true, exactlyTextLineSpace.ExactlyLineHeight, 0);
        }
    }
}
