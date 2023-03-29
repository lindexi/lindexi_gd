using LightTextEditorPlus.Core.Document;

using System;
using System.Collections.Generic;
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
    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        // 如果尺寸不足，也就是一个都拿不到
        return new SingleCharInLineLayoutResult(takeCount: 0, default, charSizeList: default);
    }
}


internal interface IInternalCharDataSizeMeasurer
{
    Size GetCharSize(CharData charData);
}