using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 相对于段落的坐标点
/// </summary>
/// todo 改名 TextPointInParagraphCoordinate
public readonly record struct TextPointInParagraph
{
    internal TextPointInParagraph(double x, double y, ParagraphData paragraphData)
    {
        _x = x;
        _y = y;
        _paragraphData = paragraphData;
    }

    //internal TextPointInParagraph(TextPoint textPointInParagraph, ParagraphData paragraphData)
    //{
    //    _paragraphPoint = textPointInParagraph;
    //    _paragraphData = paragraphData;
    //}

    //private readonly TextPoint _paragraphPoint;
    private readonly double _x;
    private readonly double _y;
    private readonly ParagraphData _paragraphData;

    /// <summary>
    /// 转换为相对于文本框的坐标
    /// </summary>
    /// <param name="paragraphData"></param>
    /// <returns></returns>
    internal TextPoint ToDocumentPoint(ParagraphData paragraphData)
    {
        return ToDocumentContentCoordinate(paragraphData).ToTextPoint();
    }

    /// <summary>
    /// 转换为相对于文本框的坐标
    /// </summary>
    /// <param name="layoutData"></param>
    /// <returns></returns>
    public TextPoint ToDocumentPoint(IParagraphLayoutData layoutData)
    {
        return ToDocumentContentCoordinate(layoutData).ToTextPoint();
    }

    /// <summary>
    /// 转换为相对于文本内容的坐标
    /// </summary>
    /// <param name="paragraphData"></param>
    /// <returns></returns>
    /// <exception cref="TextEditorDebugException"></exception>
    internal TextPointInDocumentContentCoordinate ToDocumentContentCoordinate(ParagraphData paragraphData)
    {
        Debug.Assert(ReferenceEquals(_paragraphData, paragraphData), "禁止拿其他段落获取相对的坐标点");
        if (_paragraphData.IsInDebugMode && !ReferenceEquals(_paragraphData, paragraphData))
        {
            throw new TextEditorDebugException("禁止拿其他段落获取相对的坐标点");
        }

        IParagraphLayoutData layoutData = paragraphData.ParagraphLayoutData;
        return ToDocumentContentCoordinate(layoutData);
    }

    internal TextPointInDocumentContentCoordinate ToDocumentContentCoordinate(IParagraphLayoutData layoutData)
    {
        var startPoint = layoutData.StartPointInDocumentContentCoordinate;
        TextThickness contentThickness = layoutData.TextContentThickness;

        if (_paragraphData.IsInDebugMode)
        {
            if (contentThickness.IsInvalid)
            {
                throw new TextEditorDebugException($"将段落坐标转换为文档坐标时，要求段落已经设置了正确的边距");
            }
        }

        return startPoint.Offset(_x + contentThickness.Left, _y + contentThickness.Top);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"段内坐标：{_x:0.###},{_y:0.###}";
    }

    /// <summary>
    /// 相对的增加
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public TextPointInParagraph Offset(double x, double y)
    {
        return new TextPointInParagraph(x + _x, y + _y, _paragraphData);
    }

    /// <summary>
    /// 重置 X 坐标，保持 Y 不变
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public TextPointInParagraph ResetX(double x)
    {
        return new TextPointInParagraph(x, _y, _paragraphData);
    }

    public TextPointInParagraph ResetY(double y)
    {
        return new TextPointInParagraph(_x, y, _paragraphData);
    }
}