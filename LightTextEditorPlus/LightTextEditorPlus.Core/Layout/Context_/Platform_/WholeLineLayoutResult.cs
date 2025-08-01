using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 段内行测量布局结果
/// </summary>
/// <param name="LineSize">这一行的尺寸，包含行距</param>
/// <param name="TextSize">这一行的文本字符的尺寸，不包含行距</param>
/// <param name="CharCount">这一行使用的 字符 的数量</param>
/// <param name="LineSpacingThickness"></param>
/// <param name="MaxFontSizeCharData">最大字号的字符</param>
public readonly record struct WholeLineLayoutResult(TextSize LineSize, TextSize TextSize, int CharCount, TextThickness LineSpacingThickness, CharData MaxFontSizeCharData)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var lineHeight = LineSize.Height;
        var lineWidth = LineSize.Width;
        TextEditorInnerDebugAsset.AreEquals(lineWidth, TextSize.Width, "LineWidth");

        var textHeight = TextSize.Height;

        var lineSpacing = lineHeight - MaxFontSizeCharData.Size.Height;
        var topLineSpacingGap = LineSpacingThickness.Top;
        var bottomLineSpacingGap = LineSpacingThickness.Bottom;
        var lineSpacingGap = topLineSpacingGap + bottomLineSpacingGap;

        return
            $"行高：{lineHeight:0.##}，字高：{textHeight}，行距：{lineSpacing:0.##}，行距的空白：{lineSpacingGap:0.##}，上边距：{topLineSpacingGap:0.##}，下边距：{bottomLineSpacingGap:0.##}";
    }
}
