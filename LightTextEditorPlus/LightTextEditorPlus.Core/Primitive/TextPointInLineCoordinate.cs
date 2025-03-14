using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 相对于行的坐标点
/// </summary>
public readonly record struct TextPointInLineCoordinate
{
    /// <summary>
    /// 相对于行的坐标点
    /// </summary>
    public TextPointInLineCoordinate(double x, double y)
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
    internal TextPoint ToDocumentPoint(LineLayoutData lineLayoutData)
    {
        // 先获取行内容的相对文档的起始点
        TextPoint charStartPoint = lineLayoutData.LineContentStartPoint;
        return new TextPoint(_x + charStartPoint.X,
            _y + charStartPoint.Y);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"行内坐标： {_x:0.###},{_y:0.###}";
    }
}