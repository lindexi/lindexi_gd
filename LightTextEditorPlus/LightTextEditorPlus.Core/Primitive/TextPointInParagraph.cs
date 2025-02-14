using System.Diagnostics;
using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 相对于段落的坐标点
/// </summary>
public readonly record struct TextPointInParagraph
{
    internal TextPointInParagraph(double x, double y, ParagraphData paragraphData) : this(new TextPoint(x, y),
        paragraphData)
    {
    }

    internal TextPointInParagraph(TextPoint textPointInParagraph, ParagraphData paragraphData)
    {
        _paragraphPoint = textPointInParagraph;
        _paragraphData = paragraphData;
    }

    private readonly TextPoint _paragraphPoint;
    private readonly ParagraphData _paragraphData;

    /// <summary>
    /// 转换为相对于文本框的坐标
    /// </summary>
    /// <param name="paragraphData"></param>
    /// <returns></returns>
    internal TextPoint ToDocumentPoint(ParagraphData paragraphData)
    {
        Debug.Assert(ReferenceEquals(_paragraphData, paragraphData), "禁止哪其他段落获取相对的坐标点");

        IParagraphLayoutData layoutData = paragraphData.ParagraphLayoutData;
        return ToDocumentPoint(layoutData);
    }

    /// <summary>
    /// 转换为相对于文本框的坐标
    /// </summary>
    /// <param name="layoutData"></param>
    /// <returns></returns>
    public TextPoint ToDocumentPoint(IParagraphLayoutData layoutData)
    {
        TextRect bounds = layoutData.OutlineBounds;
        return new TextPoint(_paragraphPoint.X + bounds.X, _paragraphPoint.Y + bounds.Y);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"段内坐标：{_paragraphPoint.X:0.00},{_paragraphPoint.Y:0.00}";
    }

    /// <summary>
    /// 相对的增加
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public TextPointInParagraph Add(double x, double y)
    {
        return new TextPointInParagraph(x + _paragraphPoint.X, y + _paragraphPoint.Y, _paragraphData);
    }
}