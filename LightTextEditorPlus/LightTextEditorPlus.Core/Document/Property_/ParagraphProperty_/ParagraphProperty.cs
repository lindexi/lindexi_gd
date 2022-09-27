
using LightTextEditorPlus.Core.Primitive;

using System;

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
    /// 行间距倍数，默认值为1，范围0~1000
    /// </summary>
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
    /// 段前间距
    /// </summary>
    public double ParagraphBefore
    {
        get;
        init;
    }

    /// <summary>
    /// 段后间距
    /// </summary>
    public double ParagraphAfter
    {
        get;
        init;
    }
}