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
    /// [dotnet OpenXML 聊聊 PPT 文本行距行高计算公式](https://blog.lindexi.com/post/dotnet-OpenXML-%E8%81%8A%E8%81%8A-PPT-%E6%96%87%E6%9C%AC%E8%A1%8C%E8%B7%9D%E8%A1%8C%E9%AB%98%E8%AE%A1%E7%AE%97%E5%85%AC%E5%BC%8F.html )
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateLineHeightWithPPTLineSpacingAlgorithm(double lineSpacing, double fontSize)
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        // 以下是算法
        const double PPTFL = PPTFontLineSpacing;
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

    /// <summary>
    /// 将 PPT 的行距转换为 WPF 的行距
    /// </summary>
    /// <returns></returns>
    /// 算法：
    /// 根据 <see cref="CalculateLineHeightWithWPFLineSpacingAlgorithm"/> 和 <see cref="CalculateLineHeightWithPPTLineSpacingAlgorithm"/> 的算法可以推导出：
    /// WPFPixelLineSpacing = FontSize * FontFamilyLineSpacing * (LineSpace - 1) / 10 + FontSize * FontFamilyLineSpacing
    /// 假定 WPFPixelLineSpacing 和 PPT 的相同，则有：
    /// PPTPixelLineSpacing = (a * PPTFL * OriginLineSpacing + b) * FontSize
    /// 其中 PPT 的行距计算的 a 和 b 为一次线性函数的方法，而 PPTFL 是 PPT Font Line Spacing 的意思，在 PPT 所有文字的行距都是这个值
    /// 可以将 a 和 PPTFL 合并为 PPTFL 然后使用 a 代替，此时 a 和 b 是常量
    /// PPTPixelLineSpacing = (a * OriginLineSpacing + b) * FontSize
    /// 假定 WPFPixelLineSpacing 和 PPT 的相同，那么这样计算
    ///   PPTPixelLineSpacing = WPFPixelLineSpacing
    /// (a * OriginLineSpacing + b) * FontSize = FontSize * FontFamilyLineSpacing * (LineSpace - 1) / 10 + FontSize * FontFamilyLineSpacing
    /// 两边除以 FontSize 字体大小
    /// (a * OriginLineSpacing + b) = FontFamilyLineSpacing * (LineSpace - 1) / 10 + FontFamilyLineSpacing
    /// 进行数学计算
    ///                                      (a * OriginLineSpacing + b) = FontFamilyLineSpacing * (LineSpace - 1) / 10 + FontFamilyLineSpacing
    /// (a * OriginLineSpacing + b) - FontFamilyLineSpacing = FontFamilyLineSpacing * (LineSpace - 1) / 10
    /// ((a * OriginLineSpacing + b) - FontFamilyLineSpacing) * 10 = FontFamilyLineSpacing * (LineSpace - 1)
    /// (((a * OriginLineSpacing + b) - FontFamilyLineSpacing) * 10) / FontFamilyLineSpacing = (LineSpace - 1)
    /// (((a * OriginLineSpacing + b) - FontFamilyLineSpacing) * 10) / FontFamilyLineSpacing + 1 = LineSpace
    /// 而常量 a 和 b 的值如下
    /// a = 1.2018;
    /// b = 0.0034;
    /// PPTFontLineSpacing = a;
    /// 详细请参阅：
    /// [dotnet OpenXML 聊聊 PPT 文本行距行高计算公式](https://blog.lindexi.com/post/dotnet-OpenXML-%E8%81%8A%E8%81%8A-PPT-%E6%96%87%E6%9C%AC%E8%A1%8C%E8%B7%9D%E8%A1%8C%E9%AB%98%E8%AE%A1%E7%AE%97%E5%85%AC%E5%BC%8F.html )
    public static double ConvertPptLineSpacingToWPFLineSpacing(double pptOriginLineSpacing,
        double fontFamilyLineSpacing)
    {
        return (((PPTFontLineSpacing * pptOriginLineSpacing + 0.0034) - fontFamilyLineSpacing) * 10) / fontFamilyLineSpacing + 1;
    }

    /// <summary>
    /// 这是一个常量，计算方法请参阅 [dotnet OpenXML 聊聊 PPT 文本行距行高计算公式](https://blog.lindexi.com/post/dotnet-OpenXML-%E8%81%8A%E8%81%8A-PPT-%E6%96%87%E6%9C%AC%E8%A1%8C%E8%B7%9D%E8%A1%8C%E9%AB%98%E8%AE%A1%E7%AE%97%E5%85%AC%E5%BC%8F.html )
    /// </summary>
    private const double PPTFontLineSpacing = 1.2018;
}