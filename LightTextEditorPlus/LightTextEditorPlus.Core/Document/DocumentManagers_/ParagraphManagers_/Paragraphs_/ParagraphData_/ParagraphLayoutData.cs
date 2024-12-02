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
    TextPoint StartPoint { get; }
    /// <summary>
    /// 段落尺寸
    /// </summary>
    TextSize TextSize { get; }
}

/// <summary>
/// 段落的布局数据
/// </summary>
class ParagraphLayoutData : IParagraphLayoutData
{
    /// <summary>
    /// 段落的起始点
    /// </summary>
    public TextPoint StartPoint { set; get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    public TextSize TextSize { set; get; }

    /// <summary>
    /// 段落的范围
    /// </summary>
    /// <returns></returns>
    public TextRect GetBounds() => new TextRect(StartPoint, TextSize);
}