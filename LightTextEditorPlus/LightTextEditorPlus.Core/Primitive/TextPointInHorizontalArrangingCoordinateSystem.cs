using System;
using System.Diagnostics;
using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 按照水平从左到右排列的坐标系的点，使用 TextEditor 坐标系
/// </summary>
public readonly struct TextPointInHorizontalArrangingCoordinateSystem
{
    /// <summary>
    /// 相对于水平排列的坐标系的点
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="layoutManager"></param>
    internal TextPointInHorizontalArrangingCoordinateSystem(double x, double y, LayoutManager layoutManager)
    {
        _x = x;
        _y = y;
        _layoutManager = layoutManager;
    }

    /// <summary>
    /// 零点
    /// </summary>
    /// <param name="layoutManager"></param>
    /// <returns></returns>
    internal static TextPointInHorizontalArrangingCoordinateSystem Zero(LayoutManager layoutManager)
        // 零点不一定是最终的零点，如从右到左布局下，零点是在右上角
        =>
        new TextPointInHorizontalArrangingCoordinateSystem(0, 0, layoutManager);

    private readonly double _x;

    private readonly double _y;

    private TextEditorCore TextEditor => _layoutManager.TextEditor;
    private readonly LayoutManager _layoutManager;

    /// <summary>
    /// 偏移
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public TextPointInHorizontalArrangingCoordinateSystem Offset(double x, double y)
    {
        return new TextPointInHorizontalArrangingCoordinateSystem(_x + x, _y + y, _layoutManager);
    }

    /// <summary>
    /// 转换为按照当前排版类型的坐标，如果是竖排，则转换为竖排坐标
    /// </summary>
    /// <returns></returns>
    public TextPoint ToCurrentArrangingTypePoint()
    {
        TextEditor.VerifyNotDirty();

        ArrangingType arrangingType = TextEditor.ArrangingType;
        if (arrangingType.IsHorizontal)
        {
            return new TextPoint(_x, _y);
        }
        else if (arrangingType.IsLeftToRightVertical)
        {
            // 正常的从左到右的竖排，蒙文竖排格式
            Debug.Assert(arrangingType.IsVertical);
            return new TextPoint(_y, _x);
        }
        else if (!arrangingType.IsLeftToRightVertical)
        {
            // 从右到左的竖排，文言的竖排格式
            Debug.Assert(arrangingType.IsVertical);

            DocumentLayoutBoundsInHorizontalArrangingCoordinateSystem bounds = _layoutManager.DocumentLayoutBounds;
            TextSize outlineSize = bounds.DocumentOutlineSize;
            // 竖排情况下，相对于横排坐标系需要交换宽高。这里的交换只是为了让逻辑更加顺而已，也刚好是个结构体，交换一下不要钱
            outlineSize = outlineSize.SwapWidthAndHeight();
           
            var x = outlineSize.Width - _y;
            var y = _x;
            return new TextPoint(x, y);
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"相对横排 ({_x:0.##},{_y:0.##})";
    }

    internal string ToStringValueOnly() => $"{_x:0.##},{_y:0.##}";
}