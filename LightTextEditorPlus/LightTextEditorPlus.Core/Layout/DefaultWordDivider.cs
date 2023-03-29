using LightTextEditorPlus.Core.Document;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 默认的分词器
/// </summary>
internal class DefaultWordDivider
{
    public DefaultWordDivider(IInternalCharDataSizeMeasurer charDataSizeMeasurer)
    {
        _charDataSizeMeasurer = charDataSizeMeasurer;
    }

    private IInternalCharDataSizeMeasurer _charDataSizeMeasurer;

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        var charData = argument.CurrentCharData;

        Size size = GetCharSize(charData);

        // 单个字符直接布局，无视语言文化。快，但是诡异
        if (argument.LineRemainingWidth > size.Width)
        {
            return new SingleCharInLineLayoutResult(takeCount: 1, size);
        }
        else
        {
            // 如果尺寸不足，也就是一个都拿不到
            return new SingleCharInLineLayoutResult(takeCount: 0, default);
        }
    }

    [DebuggerStepThrough]
    private Size GetCharSize(CharData charData) => _charDataSizeMeasurer.GetCharSize(charData);
}


internal interface IInternalCharDataSizeMeasurer
{
    Size GetCharSize(CharData charData);
}