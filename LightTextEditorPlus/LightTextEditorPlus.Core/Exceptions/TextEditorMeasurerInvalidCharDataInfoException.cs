using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Platform;

namespace LightTextEditorPlus.Core.Exceptions;

/// <summary>
/// 通过 <see cref="ICharInfoMeasurer"/> 测量之后，发现有字符没有尺寸时抛出此异常
/// </summary>
public class TextEditorMeasurerInvalidCharDataInfoException : TextEditorException
{
    internal TextEditorMeasurerInvalidCharDataInfoException
        (TextReadOnlyListSpan<CharData> toMeasureCharDataList, int index)
    {
        CharData = toMeasureCharDataList[index];
        ToMeasureCharDataList = toMeasureCharDataList;
        Index = index;
    }

    /// <summary>
    /// 没有尺寸信息的字符
    /// </summary>
    public CharData CharData { get; }

    /// <summary>
    /// 所测量的字符列表
    /// </summary>
    public TextReadOnlyListSpan<CharData> ToMeasureCharDataList { get; }

    /// <summary>
    /// 在 <see cref="ToMeasureCharDataList"/> 的第几个字符没有尺寸
    /// </summary>
    public int Index { get; }

    /// <inheritdoc />
    public override string Message => $"测量布局之后，字符依然没有尺寸。第 {Index} 个字符：'{CharData.CharObject.ToText()}'";
}