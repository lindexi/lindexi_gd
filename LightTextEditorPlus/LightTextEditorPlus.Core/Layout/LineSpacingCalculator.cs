using System;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 行距计算器
/// </summary>
/// 框架内置的行距计算器
public static class LineSpacingCalculator
{
    /// <summary>
    /// 通过行距来计算行高
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="runProperty"></param>
    /// <param name="lineSpacing"></param>
    /// <returns></returns>
    /// <remarks>算法请参阅 <see cref="LineSpacingAlgorithm"/> 类型注释</remarks>
    public static double CalculateLineHeightWithLineSpacing(TextEditorCore textEditor, IReadOnlyRunProperty runProperty, double lineSpacing)
    {
        var fontSize = runProperty.FontSize;

        if (textEditor.LineSpacingAlgorithm == LineSpacingAlgorithm.WPF)
        {
            var fontLineSpacing = textEditor.PlatformProvider.FontLineSpacing(runProperty);
            // 以下是算法
            var lineHeight = fontSize * fontLineSpacing * (lineSpacing - 1) / 10
                         + fontSize * fontLineSpacing;

            return lineHeight;
        }
        else if (textEditor.LineSpacingAlgorithm == LineSpacingAlgorithm.PPT)
        {
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once IdentifierTypo
            // 以下是算法
            const double PPTFL = 1.2018;
            var lineHeight = (PPTFL * lineSpacing + 0.0034) * fontSize;
            return lineHeight;
        }
        else
        {
            // 理论上不会进入此分支
            throw new NotSupportedException();
        }
    }
}