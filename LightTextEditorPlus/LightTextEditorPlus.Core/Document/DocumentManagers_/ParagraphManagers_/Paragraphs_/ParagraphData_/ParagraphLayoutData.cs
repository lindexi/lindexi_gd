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
    Point StartPoint { get; }
    /// <summary>
    /// 段落尺寸
    /// </summary>
    Size Size { get; }
}

/// <summary>
/// 段落的布局数据
/// </summary>
class ParagraphLayoutData : IParagraphLayoutData
{
    /// <summary>
    /// 段落的起始点
    /// </summary>
    public Point StartPoint { set; get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    public Size Size { set; get; }

    /// <summary>
    /// 段落的范围
    /// </summary>
    /// <returns></returns>
    public Rect GetBounds() => new Rect(StartPoint, Size);
}