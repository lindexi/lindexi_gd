using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落的渲染数据
/// </summary>
class ParagraphLayoutData
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