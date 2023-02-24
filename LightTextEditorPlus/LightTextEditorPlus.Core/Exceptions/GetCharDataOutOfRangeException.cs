using System;
using System.Collections.Generic;
using System.Text;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 获取字符时坐标超过字符数量异常
/// </summary>
public class GetCharDataOutOfRangeException : TextEditorException
{
    internal GetCharDataOutOfRangeException(ParagraphData paragraph, List<CharData> charDataList, int offset,
        ArgumentOutOfRangeException exception) : base("获取字符时坐标超过字符数量", exception)
    {
        Paragraph = paragraph;
        CharDataList = charDataList;
        InputOffset = offset;

        Count = CharDataList.Count;
    }

    internal ParagraphData Paragraph { get; }

    /// <summary>
    /// 字符列表
    /// </summary>
    public IReadOnlyList<CharData> CharDataList { get; }

    /// <summary>
    /// 输入的偏移量
    /// </summary>
    public int InputOffset { get; }

    /// <summary>
    /// 总字符数量
    /// </summary>
    public int Count { get; }

    /// <inheritdoc />
    public override string Message => $"Count={Count};InputOffset={InputOffset};ParagraphIndex={Paragraph.Index};CharDataList={CharDataListToString()}";

    private string CharDataListToString()
    {
        var str = new StringBuilder();
        foreach (var charData in CharDataList)
        {
            str.Append(charData.CharObject.ToText());
        }

        return str.ToString();
    }
}