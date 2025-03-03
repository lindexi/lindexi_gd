using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 测量行内字符参数
/// </summary>
/// <param name="RunList">当前一整行的字符信息</param>
/// <param name="CurrentIndex">当前字符的序号，相对于 <see cref="RunList"/> 的序号</param>
/// <param name="LineRemainingWidth">这一行剩余的宽度</param>
/// <param name="Paragraph"></param>
/// <param name="UpdateLayoutContext"></param>
public readonly record struct SingleCharInLineLayoutArgument(TextReadOnlyListSpan<CharData> RunList, int CurrentIndex,
    double LineRemainingWidth, ITextParagraph Paragraph, UpdateLayoutContext UpdateLayoutContext)
{
    /// <summary>
    /// 当前的字符信息
    /// </summary>
    public CharData CurrentCharData => RunList[CurrentIndex];

    public ParagraphProperty ParagraphProperty => Paragraph.ParagraphProperty;

    /// <summary>
    /// 字符布局信息设置器
    /// </summary>
    public ICharDataLayoutInfoSetter CharDataLayoutInfoSetter => UpdateLayoutContext;

    /// <summary>
    /// 是否这一行啥都没有获取到，这一行还没布局到一个字符
    /// 对于这个情况下，尽管这一行放不下一个字符，但是依然还是要强行放入。否则可能下一行依然也不够空间放下
    /// </summary>
    public bool IsTakeEmpty => CurrentIndex == 0;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Current:'{CurrentCharData.CharObject.ToText()}';Index:{CurrentIndex};\r\nRunList={RunList.ToText()}";
    }
}
