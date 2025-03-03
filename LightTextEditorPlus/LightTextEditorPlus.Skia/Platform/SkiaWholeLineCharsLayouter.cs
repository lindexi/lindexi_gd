using System;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Platform;

internal class SkiaWholeLineCharsLayouter : IWholeLineCharsLayouter
{
    public WholeLineCharsLayoutResult UpdateWholeLineCharsLayout(in WholeLineLayoutArgument argument)
    {
        //TextSize CurrentLineCharTextSize, int WholeTakeCount
        // 取连续的字符，连续的字符指的是属性相同的字符。属性不相同的，分割为多次调用布局
        var wholeTakeCount = 0;

        while (wholeTakeCount < argument.CharDataList.Count)
        {
            var currentIndex = wholeTakeCount;
            TextReadOnlyListSpan<CharData> runList = argument.CharDataList.Slice(currentIndex).GetFirstCharSpanContinuous();

        }

        throw new NotImplementedException();
    }

    private SingleCharInLineLayoutResult MeasureSingleRunLayout()
    {
        throw new NotImplementedException();
    }
}