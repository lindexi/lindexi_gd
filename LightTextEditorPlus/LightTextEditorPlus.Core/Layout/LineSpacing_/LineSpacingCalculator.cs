using System;
using System.Runtime.CompilerServices;
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
            var fontLineSpacing = textEditor.PlatformProvider.GetFontLineSpacing(runProperty);
            return CalculateLineHeightWithWPFLineSpacingAlgorithm(lineSpacing, fontSize, fontLineSpacing);
        }
        else if (textEditor.LineSpacingAlgorithm == LineSpacingAlgorithm.PPT)
        {
            return CalculateLineHeightWithPPTLineSpacingAlgorithm(lineSpacing, fontSize);
        }
        else
        {
            // 理论上不会进入此分支
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// 采用 PPT 行距算法计算行高
    /// </summary>
    /// <param name="lineSpacing"></param>
    /// <param name="fontSize"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateLineHeightWithPPTLineSpacingAlgorithm(double lineSpacing, double fontSize)
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        // 以下是算法
        const double PPTFL = 1.2018;
        var lineHeight = (PPTFL * lineSpacing + 0.0034) * fontSize;
        return lineHeight;
    }

    /// <summary>
    /// 采用 WPF 行距算法计算行高
    /// </summary>
    /// <param name="lineSpacing"></param>
    /// <param name="fontSize"></param>
    /// <param name="fontLineSpacing"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateLineHeightWithWPFLineSpacingAlgorithm(double lineSpacing, double fontSize,
        double fontLineSpacing)
    {
        // 以下是算法
        var lineHeight = fontSize * fontLineSpacing * (lineSpacing - 1) / 10
                         + fontSize * fontLineSpacing;

        return lineHeight;
    }
}