using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Core.Utils.Maths;

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

    internal TextPointInDocumentContentCoordinate StartPointInDocumentContentCoordinate { get; }

    /// <summary>
    /// 段落尺寸
    /// </summary>
    TextSize TextSize { get; }

    /// <summary>
    /// 段落的文本内容的边距。一般就是段前和段后间距
    /// </summary>
    /// 为什么左右边距不叠加在段落上？现在是每一行都叠加，因为前面实现错误，以为左边距会受到悬挂缩进的影响。实际应该让左右边距放在这里，行只处理缩进
    /// 但行在处理的过程，本身就需要考虑左右边距影响了行的可用宽度，因此放在行里面处理也是可以的
    TextThickness TextContentThickness { get; }

    /// <summary>
    /// 外接的尺寸，包含段前和段后和左右边距
    /// </summary>
    TextSize OutlineSize { get; }

    /// <summary>
    /// 段落的文本范围
    /// </summary>
    TextRect TextContentBounds { get; }

    /// <summary>
    /// 外接边界，包含对齐的空白
    /// </summary>
    TextRect OutlineBounds { get; }
}

/// <summary>
/// 相对于文档内容坐标系的点
/// </summary>
public readonly struct TextPointInDocumentContentCoordinate
    :IEquatable<TextPointInDocumentContentCoordinate>
{
    internal TextPointInDocumentContentCoordinate(double x, double y, LayoutManager manager)
    {
        _x = x;
        _y = y;
        _manager = manager;
    }

    private readonly double _x;

    private readonly double _y;

    private readonly LayoutManager _manager;

    public bool IsZero => _x == 0 && _y == 0;

    public bool IsInvalid
    // 只需判断一个条件就好了，不用判断 X 和 Y 的值
        => ReferenceEquals(_manager, null);

    public static TextPointInDocumentContentCoordinate InvalidStartPoint
    {
        get
        {
            TextPoint invalidStartPoint = TextContext.InvalidStartPoint;

            return new TextPointInDocumentContentCoordinate(invalidStartPoint.X, invalidStartPoint.Y, manager: null!);
        }
    }

    public bool Equals(TextPointInDocumentContentCoordinate other)
    {
        return _x.Equals(other._x) && _y.Equals(other._y) && ReferenceEquals(_manager,other._manager);
    }

    public override bool Equals(object? obj)
    {
        return obj is TextPointInDocumentContentCoordinate other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_x, _y);
    }

    /// <summary>
    /// 转换为文档坐标系的点
    /// </summary>
    /// <returns></returns>
    public TextPoint ToTextPoint()
    {
        _manager.TextEditor.VerifyNotDirty(autoLayoutEmptyTextEditor: false);
        TextPoint documentContentStartPoint = _manager.DocumentLayoutBounds.DocumentContentBounds.Location;
        return new TextPoint(_x + documentContentStartPoint.X, _y + documentContentStartPoint.Y);
    }

    internal bool NearlyEqualsX(double x) => Nearly.Equals(_x, x);
    internal bool NearlyEqualsY(double y) => Nearly.Equals(_y, y);

    public TextPointInDocumentContentCoordinate Offset(double offsetX, double offsetY)
    {
        return new TextPointInDocumentContentCoordinate(_x + offsetX, _y + offsetY, _manager);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"DocumentContentCoordinate:[{_x:0.###},{_y:0.###}]";
    }
}

/// <summary>
/// 段落的布局数据
/// </summary>
class ParagraphLayoutData : IParagraphLayoutData
{
    /// <summary>
    /// 段落的起始点
    /// </summary>
    public TextPoint StartPoint => StartPointInDocumentContentCoordinate.ToTextPoint();

    /// <summary>
    /// 段落的起始点，相对于文档内容坐标系
    /// </summary>
    public TextPointInDocumentContentCoordinate StartPointInDocumentContentCoordinate { set; get; }

    /// <summary>
    /// 段落尺寸，包含文本的尺寸
    /// </summary>
    public TextSize TextSize { set; get; } = TextSize.Invalid;

    /// <summary>
    /// 段落的文本内容的边距。一般就是段前和段后间距
    /// </summary>
    /// 为什么左右边距不叠加在段落上？现在是每一行都叠加，因为前面实现错误，以为左边距会受到悬挂缩进的影响。实际应该让左右边距放在这里，行只处理缩进
    /// 但行在处理的过程，本身就需要考虑左右边距影响了行的可用宽度，因此放在行里面处理也是可以的
    public TextThickness TextContentThickness { get; set; } = TextThickness.Invalid;

    /// <summary>
    /// 外接的尺寸，包含段前和段后和左右边距
    /// </summary>
    public TextSize OutlineSize { get; set; } = TextSize.Invalid;

    /// <summary>
    /// 外接边界，包含对齐的空白
    /// </summary>
    /// 是否 OutlineBounds = TextBounds + ContentThickness ？ 不等于
    /// 还差哪些？ 文本的段落如果只有两个字符，则 TextSize 就只有这两个字符的宽度。而 OutlineSize 则会是整个横排文本框的宽度。段落文本尺寸 TextSize 是包含的字符的尺寸，不包含任何空白
    public TextRect OutlineBounds
    {
        get
        {
            Debug.Assert(OutlineSize!=TextSize.Invalid, "只有在回溯最终布局段落之后才能获取外接尺寸");
            return new TextRect(StartPoint, OutlineSize);
        }
    }

    /// <summary>
    /// 段落的文本范围，不包含空白
    /// </summary>
    public TextRect TextContentBounds
    {
        get
        {
            // 跳过空白部分
            TextPoint textPoint = StartPoint.Offset(TextContentThickness.Left, TextContentThickness.Top);
            return new TextRect(textPoint, TextSize);
        }
    }

    public void SetLayoutDirty(bool exceptTextSize)
    {
        StartPointInDocumentContentCoordinate = TextPointInDocumentContentCoordinate.InvalidStartPoint;
        if (!exceptTextSize)
        {
            TextSize = TextSize.Invalid;
        }
        OutlineSize = TextSize.Invalid;
        TextContentThickness = TextThickness.Invalid;
    }
}
