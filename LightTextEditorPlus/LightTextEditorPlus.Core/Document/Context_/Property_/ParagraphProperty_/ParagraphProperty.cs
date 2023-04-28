
using LightTextEditorPlus.Core.Primitive;

using System;
using System.Runtime.CompilerServices;
using TextEditor = LightTextEditorPlus.Core.TextEditorCore;

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
    // todo 首行缩进 悬挂缩进
    public IndentType IndentType { get; init; } = IndentType.FirstLine;

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
    /// 行距的倍数需要根据 <see cref="TextEditor.LineSpacingAlgorithm"/> 进行决定
    /// 另外是否加上行距计算，需要根据 <see cref="TextEditor.LineSpacingStrategy"/> 进行决定
    public double LineSpacing
    {
        get;
        init;
    } = 1;

    /// <summary>
    /// 固定行间距，默认值为Nan，范围>0,单位px
    /// 固定行间距值优先于<see cref="LineSpacing"/>
    /// </summary>
    public double FixedLineSpacing
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

    ///// <summary>
    ///// 段落文本左中右对齐方式，默认左对齐
    ///// </summary>
    //public TextAlignment TextAlignment
    //{
    //    get;
    //    set;
    //}

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
    public IReadOnlyRunProperty? ParagraphStartRunProperty
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