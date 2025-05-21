using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Layout;

internal readonly record struct ParagraphLayoutIndentInfo(double FirstLineTotalIndentation, double TotalIndentation, double LeftIndentation, double RightIndentation, double MarkerIndentation,
    double ParagraphLineUsableMaxWidth)
{
    /// <summary>
    /// 获取行可用的最大宽度
    /// </summary>
    /// <param name="isFirstLine"></param>
    /// <returns></returns>
    public double GetUsableLineMaxWidth(bool isFirstLine)
    {
        if (isFirstLine)
        {
            return ParagraphLineUsableMaxWidth - FirstLineTotalIndentation;
        }

        return ParagraphLineUsableMaxWidth - TotalIndentation;
    }
}