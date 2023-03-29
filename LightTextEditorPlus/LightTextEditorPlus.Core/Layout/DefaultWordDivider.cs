using LightTextEditorPlus.Core.Document;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils.Patterns;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 默认的分词器
/// </summary>
internal class DefaultWordDivider
{
    public DefaultWordDivider(TextEditorCore textEditor ,IInternalCharDataSizeMeasurer charDataSizeMeasurer)
    {
        _textEditor = textEditor;
        _charDataSizeMeasurer = charDataSizeMeasurer;
    }

    private readonly TextEditorCore _textEditor;
    private IInternalCharDataSizeMeasurer _charDataSizeMeasurer;
    private WordRange _currentWord;

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        // 判断是否在单词内
        var charData = argument.CurrentCharData;
        Size size = GetCharSize(charData);

        string currentCharText = charData.CharObject.ToText();
        if (currentCharText == RegexPatterns.BlankSpace)
        {
            // 如果当前是空格的话，那就判断是否可以返回啦，需要额外判断空格是否允许溢出行
            // todo 是否需要支持空格溢出行
            // 以下是无视此规则
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

        if (argument.IsTakeEmpty)
        {
            // todo 空行强行换行
            // 测试 aa about 布局
        }

        // 中文考虑支持 GB/T 15834 规范

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

    readonly record struct WordRange(ReadOnlyListSpan<CharData> RunList, int StartIndex, int EndIndex);
}

internal interface IInternalCharDataSizeMeasurer
{
    Size GetCharSize(CharData charData);
}