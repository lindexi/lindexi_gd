using System;
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
    /// <param name="textEditor"></param>
    public TextPointInHorizontalArrangingCoordinateSystem(double x, double y, TextEditorCore textEditor)
    {
        _x = x;
        _y = y;
        _textEditor = textEditor;
    }

    private readonly double _x;

    private readonly double _y;

    private readonly TextEditorCore _textEditor;

    /// <summary>
    /// 转换为按照当前排版类型的坐标，如果是竖排，则转换为竖排坐标
    /// </summary>
    /// <returns></returns>
    public TextPoint ToCurrentArrangingTypePoint()
    {
        switch (_textEditor.ArrangingType)
        {
            case ArrangingType.Horizontal:
                return new TextPoint(_x, _y);
            case ArrangingType.Vertical:
                break;
            case ArrangingType.Mongolian:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new NotSupportedException();
    }
}