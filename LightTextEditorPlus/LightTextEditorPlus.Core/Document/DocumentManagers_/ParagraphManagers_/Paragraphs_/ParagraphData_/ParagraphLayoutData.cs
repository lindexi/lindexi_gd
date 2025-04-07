using System.Diagnostics;
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
    TextPointInHorizontalArrangingCoordinateSystem StartPoint { get; }

    internal TextPointInDocumentContentCoordinateSystem StartPointInDocumentContentCoordinateSystem { get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    TextSize TextSize { get; }

    /// <summary>
    /// 段落的文本内容的边距。一般就是段前和段后间距
    /// </summary>
    /// 为什么左右边距不叠加在段落上？现在是每一行都叠加，因为前面实现错误，以为左边距会受到悬挂缩进的影响。实际应该让左右边距放在这里，行只处理缩进
    /// 但行在处理的过程，本身就需要考虑左右边距影响了行的可用宽度，因此放在行里面处理也是可以的
    TextThickness TextContentThickness { get; }

    /// <summary>
    /// 外接的尺寸，包含段前和段后和左右边距
    /// </summary>
    TextSize OutlineSize { get; }

    /// <summary>
    /// 段落的文本范围
    /// </summary>
    TextRect TextContentBounds { get; }

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
    public TextPointInHorizontalArrangingCoordinateSystem StartPoint => StartPointInDocumentContentCoordinateSystem.ToTextPoint();

    /// <summary>
    /// 段落的起始点，相对于文档内容坐标系
    /// </summary>
    public TextPointInDocumentContentCoordinateSystem StartPointInDocumentContentCoordinateSystem { set; get; }

    /// <summary>
    /// 段落尺寸，包含文本的尺寸
    /// </summary>
    public TextSize TextSize { set; get; } = TextSize.Invalid;

    /// <summary>
    /// 段落的文本内容的边距。一般就是段前和段后间距
    /// </summary>
    /// 为什么左右边距不叠加在段落上？现在是每一行都叠加，因为前面实现错误，以为左边距会受到悬挂缩进的影响。实际应该让左右边距放在这里，行只处理缩进
    /// 但行在处理的过程，本身就需要考虑左右边距影响了行的可用宽度，因此放在行里面处理也是可以的
    public TextThickness TextContentThickness
    {
        get
        {
            // 在预布局过程中，就已经计算了边距，这个赋值十分快，理论上不会出现拿不到的情况，除非拿错段落
            Debug.Assert(_textContentThickness != TextThickness.Invalid);
            return _textContentThickness;
        }
        set => _textContentThickness = value;
    }

    private TextThickness _textContentThickness = TextThickness.Invalid;

    /// <summary>
    /// 外接的尺寸，包含段前和段后和左右边距
    /// </summary>
    public TextSize OutlineSize
    {
        get
        {
            Debug.Assert(_outlineSize != TextSize.Invalid, "不能在回溯最终布局段落之前获取外接尺寸");
            return _outlineSize;
        }
        set => _outlineSize = value;
    }

    private TextSize _outlineSize = TextSize.Invalid;

    /// <summary>
    /// 外接边界，包含对齐的空白
    /// </summary>
    /// 是否 OutlineBounds = TextBounds + ContentThickness ？ 不等于
    /// 还差哪些？ 文本的段落如果只有两个字符，则 TextSize 就只有这两个字符的宽度。而 OutlineSize 则会是整个横排文本框的宽度。段落文本尺寸 TextSize 是包含的字符的尺寸，不包含任何空白
    public TextRect OutlineBounds
    {
        get
        {
            Debug.Assert(OutlineSize != TextSize.Invalid, "只有在回溯最终布局段落之后才能获取外接尺寸");
            return new TextRect(StartPoint.ToCurrentArrangingTypePoint(), OutlineSize);
        }
    }

    /// <summary>
    /// 段落的文本范围，不包含空白
    /// </summary>
    public TextRect TextContentBounds
    {
        get
        {
            // 跳过空白部分
            TextPoint textPoint = StartPoint.Offset(TextContentThickness.Left, TextContentThickness.Top).ToCurrentArrangingTypePoint();
            return new TextRect(textPoint, TextSize);
        }
    }

    public void SetLayoutDirty(bool exceptTextSize)
    {
        StartPointInDocumentContentCoordinateSystem = TextPointInDocumentContentCoordinateSystem.InvalidStartPoint;
        if (!exceptTextSize)
        {
            TextSize = TextSize.Invalid;
        }
        OutlineSize = TextSize.Invalid;
        TextContentThickness = TextThickness.Invalid;
    }
}
