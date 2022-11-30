using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 段落的渲染数据
/// </summary>
class ParagraphLayoutData
{
    public Point StartPoint { set; get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    public Size Size { set; get; }

    public Rect GetBounds() => new Rect(StartPoint, Size);
}