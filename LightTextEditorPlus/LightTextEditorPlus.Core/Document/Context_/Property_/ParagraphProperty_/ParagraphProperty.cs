
using LightTextEditorPlus.Core.Primitive;

using System;

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
    public FlowDirection Direction
    {
        get;
        [Obsolete("当前还不支持设置从右到左")]
        init;
    } = FlowDirection.LeftToRight;

    /// <summary>
    /// 缩进度量值
    /// </summary>
    /* 在 Word 里面是 `<w:ind w:leftChars="200" w:left="7020" w:hangingChars="1100" w:hanging="6600" />` 具体是 DocumentFormat.OpenXml.Wordprocessing.Indentation 类型。分别有 FirstLine 和 Hanging 两种缩进。左右侧缩进也在 DocumentFormat.OpenXml.Wordprocessing.Indentation 类型里
      在 Word 里面，同时采用厘米和字符单位。但在文本库里面不适合使用字符单位，因为字符单位直接关联了字体和语言文化。这也是 Word 设计给排版人员挖的坑，经常遇到排版人员吐槽说缩进宽度不正确的问题。如设置首行缩进为 2 字符，然后设置两段的字号不相同，甚至只有首个字符的字号不相同，那么将看到缩进宽度不相同，这个问题让许多排版人员很头疼
      为了减少文本库的心智负担，这里依然采用无单位，保持和其他属性一致
     
      在 PPT 里面使用正数和负数来表示首行缩进和悬挂缩进，正数表示首行缩进，负数表示悬挂缩进
      首行缩进+文本之前（左边距）： `<a:pPr marL="1600000" indent="457200" />`
      悬挂缩进+文本之前（左边距）： `<a:pPr marL="1166000" indent="-457200" />`
      缩进设置都在 DocumentFormat.OpenXml.Drawing.ParagraphProperties 类型里面，分别是 LeftMargin 和 RightMargin 和 Indent 属性。其中界面里面没有提供 RightMargin 的设置，且将 LeftMargin 命名为 `文本之前` 设置项
    */
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
    public double MarginLeft => 0;

    /// <summary>
    /// 左侧缩进
    /// </summary>
    /// Word 里 Indentation indentation1 = new Indentation(){ Left = "2835", Right = "1134" };
    /// PPT 里是 LeftMargin 和 RightMargin 属性
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
        init;
    } = HorizontalTextAlignment.Left;

    // 原本设计是将 段落起始的文本格式 放在这里，但是这会导致段落属性需要不断被更新。每次更改段落首个字符的时候，都需要更新段落属性。这样太亏了，所以放弃这个设计
    // 尽管 段落起始的文本格式 放在段落属性这里是设计正确的，但为了维持不可变性和均衡内存压力性能，所以放弃这个设计
    ///// <summary>
    ///// 段落起始的文本格式，表示在段落第一个字符之前的样式
    ///// </summary>
    //public required IReadOnlyRunProperty ParagraphStartRunProperty
    //{
    //    init;
    //    get;
    //}

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