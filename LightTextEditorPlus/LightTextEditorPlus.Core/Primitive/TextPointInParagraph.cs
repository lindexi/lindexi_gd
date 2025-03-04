using System.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;

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
        Debug.Assert(ReferenceEquals(_paragraphData, paragraphData), "禁止拿其他段落获取相对的坐标点");
        if (_paragraphData.IsInDebugMode && !ReferenceEquals(_paragraphData, paragraphData))
        {
            throw new TextEditorDebugException("禁止拿其他段落获取相对的坐标点");
        }

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
        TextPoint startPoint = layoutData.StartPoint;
        TextThickness contentThickness = layoutData.TextContentThickness;
        if (_paragraphData.IsInDebugMode)
        {
            if (contentThickness.IsInvalid)
            {
                throw new TextEditorDebugException($"将段落坐标转换为文档坐标时，要求段落已经设置了正确的边距");
            }
        }

        return new TextPoint(_paragraphPoint.X + startPoint.X + contentThickness.Left, _paragraphPoint.Y + startPoint.Y + contentThickness.Top);
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
    public TextPointInParagraph Offset(double x, double y)
    {
        return new TextPointInParagraph(x + _paragraphPoint.X, y + _paragraphPoint.Y, _paragraphData);
    }
}