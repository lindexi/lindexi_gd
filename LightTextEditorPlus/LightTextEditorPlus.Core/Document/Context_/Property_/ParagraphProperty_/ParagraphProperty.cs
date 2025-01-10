
using LightTextEditorPlus.Core.Primitive;

using System;
using System.Runtime.CompilerServices;

namespace LightTextEditorPlus.Core.Document;

///// <summary>
///// 段落属性
///// </summary>
//public interface IReadonlyParagraphProperty
//{
//    /// <summary>
//    /// 构建新的属性
//    /// </summary>
//    /// <param name="action"></param>
//    /// <returns></returns>
//    IReadonlyParagraphProperty BuildNewProperty(Action<ParagraphProperty> action);
//}

/// <summary>
/// 段落属性
/// </summary>
public record ParagraphProperty
{
    /// <summary>
    /// 文本从右向左布局还是从左向右布局
    /// </summary>
    public FlowDirection Direction { get; init; } = FlowDirection.LeftToRight;

    /// <summary>
    /// 缩进度量值
    /// </summary>
    public double Indent { get; init; } = 0;

    /// <summary>
    /// 缩进类型
    /// </summary>
    /// todo 首行缩进 悬挂缩进
    /// 和 <see cref="LineSpacingStrategy"/> 不相同哦，这个是缩进的类型，而不是行距的类型。可以理解为 <see cref="LineSpacingStrategy"/> 的首行 <see cref="LineSpacingStrategy.FirstLineShrink"/> 处理的是整个文本的首行，而这个是处理段落的首行。且 <see cref="LineSpacingStrategy"/> 处理的是垂直方向的行距，而这个是处理段落的水平方向的缩进
    public IndentType IndentType { get; init; } = IndentType.FirstLine;

    /// <summary>
    /// 此属性只是为了告诉你应该使用 LeftIndentation 属性
    /// </summary>
    [Obsolete("此属性只是为了告诉你应该使用 LeftIndentation 属性")]
    public double MarginLeft => throw new NotSupportedException();

    /// <summary>
    /// 左侧缩进
    /// </summary>
    /// Word 里 Indentation indentation1 = new Indentation(){ Left = "2835", Right = "1134" };
    public double LeftIndentation { get; init; }

    /// <summary>
    /// 右侧缩进
    /// </summary>
    public double RightIndentation { get; init; }

    /// <summary>
    /// 是否允许标点溢出边界
    /// <para>
    /// 这个功能是在一段文本排版时，在接近末尾，如果再加上标点符号，将会超过文本的约束宽度。如果此属性设置为 true 将会允许加上标点符号之后的文本段超过约束宽度
    /// </para>
    /// </summary>
    /// todo 实现允许标点溢出边界
    public bool AllowHangingPunctuation { get; init; } = false;

    /// <summary>
    /// 是否允许空格溢出边界
    /// </summary>
    /// todo 实现允许空格溢出边界
    public bool AllowHangingSpace { get; init; } = false;

    /// <summary>
    /// 行间距倍数，默认值为1，范围0~1000
    /// </summary>
    /// 行距的倍数需要根据 <see cref="LineSpacingAlgorithm"/> 进行决定
    /// 另外是否加上行距计算，需要根据 <see cref="LineSpacingStrategy"/> 进行决定
    public double LineSpacing
    {
        get;
        init;
    } = 1;

    /// <summary>
    /// 固定行间距，默认值为Nan，范围>0,单位px
    /// 在文本库中，实际上是 <see cref="FixedLineHeight"/> 的作用，此属性完全和 <see cref="FixedLineHeight"/> 相同
    /// 固定行间距值优先于<see cref="LineSpacing"/>
    /// </summary>
    /// <remarks>
    /// 此属性完全和 <see cref="FixedLineHeight"/> 属性相同，只是为了更好地描述，所以保留了这个属性
    /// </remarks>
    public double FixedLineSpacing
    {
        get => FixedLineHeight;
        init => FixedLineHeight = value;
    }

    /// <summary>
    /// 固定行高，默认值为Nan，范围>0,单位px
    /// 固定行高值优先于<see cref="LineSpacing"/>
    /// 一旦设置了固定行高，将会忽略<see cref="LineSpacing"/>
    /// </summary>
    /// 在 Word 里面，称为行距固定值，使用 LineSpacingRuleValues.Exact 表示
    /// 根据文档 https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.wordprocessing.linespacingrulevalues?view=openxml-3.0.1 所述
    /// Exact： Exact Line Height.
    /// 在 PPT 里面，放在 DocumentFormat.OpenXml.Drawing.LineSpacing 里面的 SpacingPoints 里
    /// 根据文档 https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.drawing.spacingpoints?view=openxml-3.0.1 和 ISO/IEC 29500 规范所述和 ECMA 376 21.1.2.2.12 规范所述
    /// spcPts (Spacing Points)： This element specifies the amount of white space that is to be used between lines and paragraphs in the form of a text point size. The size is specified using points where 100 is equal to 1 point font and 1200 is equal to 12 point.
    public double FixedLineHeight
    {
        get;
        init;
    } = double.NaN;

    ///// <summary>
    ///// 项目符号
    ///// </summary>
    //internal MarkerProperty MarkerProperty
    //{
    //    get;
    //    set;
    //} = new MarkerProperty();

    /// <summary>
    /// 段落文本左中右对齐方式，默认左对齐
    /// </summary>
    public HorizontalTextAlignment HorizontalTextAlignment
    {
        get;
        [Obsolete("当前还没实现，请不要调用")]
        init;
    } = HorizontalTextAlignment.Left;

    ///// <summary>
    ///// 段落的默认格式，默认值为NewTextRunProperty
    ///// </summary>
    //public RunProperty DefaultRunProperty
    //{
    //    get;
    //    set;
    //} = new RunProperty();

    /// <summary>
    /// 段落起始的文本格式，表示在段落第一个字符之前的样式
    /// </summary>
    public required IReadOnlyRunProperty ParagraphStartRunProperty
    {
        init;
        get;
    }

    /// <summary>
    /// 段前间距（竖方向）
    /// </summary>
    public double ParagraphBefore
    {
        get;
        init;
    }

    /// <summary>
    /// 段后间距（竖方向）
    /// </summary>
    public double ParagraphAfter
    {
        get;
        init;
    }

    /// <summary>
    /// 判断传入是否合法
    /// </summary>
    internal void Verify()
    {
        if (Direction != FlowDirection.LeftToRight)
        {
            throw new NotSupportedException($"Not Support {Direction}");
        }
    }
}
