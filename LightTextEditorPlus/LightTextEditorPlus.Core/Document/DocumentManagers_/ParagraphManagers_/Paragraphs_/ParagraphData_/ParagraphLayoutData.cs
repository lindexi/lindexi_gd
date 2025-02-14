using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落的布局数据
/// </summary>
public interface IParagraphLayoutData
{
    /// <summary>
    /// 段落的起始点
    /// </summary>
    TextPoint StartPoint { get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    TextSize TextSize { get; }

    /// <summary>
    /// 段落的文本范围
    /// </summary>
    TextRect TextBounds { get; }

    /// <summary>
    /// 外接边界，包含对齐的空白
    /// </summary>
    TextRect OutlineBounds { get; }
}

/// <summary>
/// 段落的布局数据
/// </summary>
class ParagraphLayoutData : IParagraphLayoutData
{
    /// <summary>
    /// 段落的起始点
    /// </summary>
    public TextPoint StartPoint { set; get; }

    /// <summary>
    /// 段落尺寸，包含文本的尺寸
    /// </summary>
    public TextSize TextSize { set; get; }

    /// <summary>
    /// 段落的文本内容的边距。一般就是段前和段后距离
    /// </summary>
    /// 为什么左右边距不叠加在段落上？现在是每一行都叠加，因为前面实现错误，以为左边距会受到悬挂缩进的影响。实际应该让左右边距放在这里，行只处理缩进
    /// 但行在处理的过程，本身就需要考虑左右边距影响了行的可用宽度，因此放在行里面处理也是可以的
    public TextThickness ContentThickness { get; set; }

    /// <summary>
    /// 外接的尺寸，包含段前和段后和左右边距
    /// </summary>
    public TextSize OutlineSize { get; set; }

    /// <summary>
    /// 外接边界，包含对齐的空白
    /// </summary>
    /// 是否 OutlineBounds = TextBounds + ContentThickness ？ 不等于
    /// 还差哪些？ 文本的段落如果只有两个字符，则 TextSize 就只有这两个字符的宽度。而 OutlineSize 则会是整个横排文本框的宽度。段落文本尺寸 TextSize 是包含的字符的尺寸，不包含任何空白
    public TextRect OutlineBounds => new TextRect(StartPoint, OutlineSize);

    /// <summary>
    /// 段落的文本范围，不包含空白
    /// </summary>
    public TextRect TextBounds
    {
        get
        {
            TextPoint textPoint = StartPoint.Offset(ContentThickness.Left, ContentThickness.Top);
            return new TextRect(textPoint, TextSize);
        }
    }
}
