
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落属性
/// </summary>
public interface IReadonlyParagraphProperty
{

}

/// <summary>
/// 段落属性
/// </summary>
class ParagraphProperty : IReadonlyParagraphProperty
{
    /// <summary>
    /// 文本从右向左布局还是从左向右布局
    /// </summary>
    public FlowDirection Direction { get; set; } = FlowDirection.LeftToRight;

    /// <summary>
    /// 缩进度量值
    /// </summary>
    public double Indent { get; set; } = 0;

    /// <summary>
    /// 行间距倍数，默认值为1，范围0~1000
    /// </summary>
    public double LineSpacing
    {
        get;
        set;
    } = 1;

    /// <summary>
    /// 固定行间距，默认值为Nan，范围>0,单位px
    /// 固定行间距值优先于<see cref="LineSpacing"/>
    /// </summary>
    public double FixedLineSpacing
    {
        get;
        set;
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
        set;
    }

    /// <summary>
    /// 段后间距
    /// </summary>
    public double ParagraphAfter
    {
        get;
        set;
    }
}