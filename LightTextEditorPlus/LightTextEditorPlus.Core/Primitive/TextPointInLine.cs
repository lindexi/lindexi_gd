using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 相对于行的坐标点
/// </summary>
public readonly record struct TextPointInLine
{
    /// <summary>
    /// 相对于行的坐标点
    /// </summary>
    public TextPointInLine(double x, double y):this(new TextPoint(x, y))
    {
    }

    /// <summary>
    /// 相对于行的坐标点
    /// </summary>
    public TextPointInLine(TextPoint textPointInLine)
    {
        _linePoint = textPointInLine;
    }

    private readonly TextPoint _linePoint;

    /// <summary>
    /// 转换为相对于文本框的坐标
    /// </summary>
    /// <param name="lineLayoutData"></param>
    /// <returns></returns>
    internal TextPoint ToDocumentPoint(LineLayoutData lineLayoutData)
    {
        // 先获取行的相对文档的起始点
        TextPoint charStartPoint = lineLayoutData.CharStartPoint;
        return new TextPoint(_linePoint.X + charStartPoint.X,
            _linePoint.Y + charStartPoint.Y);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"行内坐标： {_linePoint.X:0.00},{_linePoint.Y:0.00}";
    }
}