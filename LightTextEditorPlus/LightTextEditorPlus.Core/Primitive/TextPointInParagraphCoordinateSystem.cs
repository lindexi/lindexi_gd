﻿using System.Diagnostics;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Exceptions;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 相对于段落的坐标点
/// </summary>
public readonly record struct TextPointInParagraphCoordinateSystem
{
    internal TextPointInParagraphCoordinateSystem(double x, double y, ParagraphData paragraphData)
    {
        _x = x;
        _y = y;
        _paragraphData = paragraphData;
    }

    //internal TextPointInParagraphCoordinateSystem(TextPoint textPointInParagraph, ParagraphData paragraphData)
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
    internal TextPointInHorizontalArrangingCoordinateSystem ToDocumentPoint(ParagraphData paragraphData)
    {
        return ToDocumentContentCoordinateSystem(paragraphData).ToTextPoint();
    }

    /// <summary>
    /// 转换为相对于文本框的坐标
    /// </summary>
    /// <param name="layoutData"></param>
    /// <returns></returns>
    public TextPointInHorizontalArrangingCoordinateSystem ToDocumentPoint(IParagraphLayoutData layoutData)
    {
        return ToDocumentContentCoordinateSystem(layoutData).ToTextPoint();
    }

    /// <summary>
    /// 转换为相对于文本内容的坐标
    /// </summary>
    /// <param name="paragraphData"></param>
    /// <returns></returns>
    /// <exception cref="TextEditorDebugException"></exception>
    internal TextPointInDocumentContentCoordinateSystem ToDocumentContentCoordinateSystem(ParagraphData paragraphData)
    {
        Debug.Assert(ReferenceEquals(_paragraphData, paragraphData), "禁止拿其他段落获取相对的坐标点");
        if (_paragraphData.IsInDebugMode && !ReferenceEquals(_paragraphData, paragraphData))
        {
            throw new TextEditorDebugException("禁止拿其他段落获取相对的坐标点");
        }

        IParagraphLayoutData layoutData = paragraphData.ParagraphLayoutData;
        return ToDocumentContentCoordinateSystem(layoutData);
    }

    internal TextPointInDocumentContentCoordinateSystem ToDocumentContentCoordinateSystem(IParagraphLayoutData layoutData)
    {
        var startPoint = layoutData.StartPointInDocumentContentCoordinateSystem;
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
    public TextPointInParagraphCoordinateSystem Offset(double x, double y)
    {
        return new TextPointInParagraphCoordinateSystem(x + _x, y + _y, _paragraphData);
    }

    /// <summary>
    /// 重置 X 坐标，保持 Y 不变
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public TextPointInParagraphCoordinateSystem ResetX(double x)
    {
        return new TextPointInParagraphCoordinateSystem(x, _y, _paragraphData);
    }

    /// <summary>
    /// 重置 Y 坐标，保持 X 不变
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    public TextPointInParagraphCoordinateSystem ResetY(double y)
    {
        return new TextPointInParagraphCoordinateSystem(_x, y, _paragraphData);
    }
}