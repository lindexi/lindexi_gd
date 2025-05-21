using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Core.Layout;

internal readonly record struct ParagraphLayoutIndentInfo(double FirstLineTotalIndentation, double TotalIndentation, double LeftIndentation, double RightIndentation, double MarkerIndentation,
    double ParagraphLineUsableMaxWidth)
{
    public double GetUsableLineMaxWidth(bool isFirstLine)
    {
        if (isFirstLine)
        {
            return ParagraphLineUsableMaxWidth - FirstLineTotalIndentation;
        }

        return ParagraphLineUsableMaxWidth - TotalIndentation;
    }
}