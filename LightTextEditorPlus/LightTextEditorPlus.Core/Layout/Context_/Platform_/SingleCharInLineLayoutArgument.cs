using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

/// <summary>
/// 测量行内字符参数
/// </summary>
/// <param name="RunList">当前一整行的字符信息，甚至可以认为是一整段字符。这是因为设计上认为单字符行内布局过程中，可能由于语言文化缘故，需要获取 <see cref="CurrentIndex"/> 前后周围的字符才能决定，因此决定给其更大的字符范围</param>
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

    /// <summary>
    /// 从当前的 <see cref="CurrentIndex"/> 开始拆分出新的列表
    /// </summary>
    /// <returns></returns>
    public TextReadOnlyListSpan<CharData> SliceFromCurrentRunList()=> RunList.Slice(CurrentIndex);

    /// <summary>
    /// 段落的属性
    /// </summary>
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
