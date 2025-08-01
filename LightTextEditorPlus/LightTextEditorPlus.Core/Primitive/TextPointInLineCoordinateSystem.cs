using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 相对于行的坐标点
/// </summary>
public readonly record struct TextPointInLineCoordinateSystem
{
    /// <summary>
    /// 相对于行的坐标点
    /// </summary>
    public TextPointInLineCoordinateSystem(double x, double y)
    {
        _x = x;
        _y = y;
    }

    private readonly double _x;

    private readonly double _y;

    /// <summary>
    /// 转换为相对于文本框的坐标
    /// </summary>
    /// <param name="lineLayoutData"></param>
    /// <returns></returns>
    internal TextPointInHorizontalArrangingCoordinateSystem ToDocumentPoint(LineLayoutData lineLayoutData)
    {
        // 先获取行内容的相对文档的起始点
        var charStartPoint = lineLayoutData.LineContentStartPoint;
        return charStartPoint.Offset(_x, _y);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"行内坐标： {_x:0.###},{_y:0.###}";
    }
}