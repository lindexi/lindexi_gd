
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
    public IndentType IndentType { get; init; } = IndentType.Hanging;

    /// <summary>
    /// 此属性只是为了告诉你应该使用 LeftIndentation 属性
    /// </summary>
    [Obsolete("此属性只是为了告诉你应该使用 LeftIndentation 属性", true)]
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
    /// 行距，默认值为1倍行距。可使用 <see cref="TextLineSpacings"/> 静态方法进行创建和赋值
    /// </summary>
    /// <remarks>
    /// 具体行距行为受到 <see cref="TextEditorCore.LineSpacingConfiguration"/> 配置的影响
    /// 行距的倍数需要根据 <see cref="LineSpacingAlgorithm"/> 进行决定
    /// 另外是否加上行距计算，需要根据 <see cref="LineSpacingStrategy"/> 进行决定
    /// </remarks>
    public ITextLineSpacing LineSpacing { get; init; } = TextLineSpacings.SingleLineSpace();

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
