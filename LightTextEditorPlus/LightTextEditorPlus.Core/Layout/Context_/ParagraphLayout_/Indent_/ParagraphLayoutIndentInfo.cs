using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

internal readonly record struct ParagraphLayoutIndentInfo(double LeftIndentation, double RightIndentation,
    double Indent,
    IndentType IndentType,
    double MarkerIndentation,
    double LineMaxWidth)
{
    /// <summary>
    /// 首行总的缩进量。等于左缩进+右缩进+项目符号缩进+首行缩进
    /// </summary>
    public double FirstLineTotalIndentation =>
        LeftIndentation + RightIndentation + MarkerIndentation + GetIndent(isFirstLine: true);

    /// <summary>
    /// 其他行的缩进量。等于左缩进+右缩进+项目符号缩进+悬挂缩进
    /// </summary>
    public double TotalIndentation => LeftIndentation + RightIndentation + MarkerIndentation + GetIndent(isFirstLine: false);

    /// <summary>
    /// 获取行可用的最大宽度
    /// </summary>
    /// <param name="isFirstLine"></param>
    /// <returns></returns>
    public double GetUsableLineMaxWidth(bool isFirstLine)
    {
        if (isFirstLine)
        {
            return LineMaxWidth - FirstLineTotalIndentation;
        }

        return LineMaxWidth - TotalIndentation;
    }

    /// <summary>
    /// 获取缩进
    /// </summary>
    /// <param name="isFirstLine">是否首行</param>
    /// <returns></returns>
    private double GetIndent(bool isFirstLine)
    {
        double indent = IndentType switch
        {
            // 首行缩进
            IndentType.FirstLine => isFirstLine ? Indent : 0,
            // 悬挂缩进，首行不缩进
            IndentType.Hanging => isFirstLine ? 0 : Indent,
            _ => 0
        };
        return indent;
    }
}