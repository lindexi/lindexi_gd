using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落的布局数据
/// </summary>
public interface IParagraphLayoutData
{
    ///// <summary>
    ///// 段落的起始点
    ///// </summary>
    //TextPoint StartPoint { get; }
    ///// <summary>
    ///// 段落尺寸
    ///// </summary>
    //TextSize TextSize { get; }

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
    ///// <summary>
    ///// 段落的起始点
    ///// </summary>
    //public TextPoint StartPoint { set; get; }

    ///// <summary>
    ///// 段落尺寸
    ///// </summary>
    //public TextSize TextSize { set; get; }

    /// <summary>
    /// 外接边界，包含对齐的空白
    /// </summary>
    public TextRect OutlineBounds { set; get; }

    /// <summary>
    /// 段落的文本范围，不包含空白
    /// </summary>
    /// <returns></returns>
    public TextRect TextBounds { get; set; }
    //public TextRect GetTextBounds() => new TextRect(StartPoint, TextSize);
}
