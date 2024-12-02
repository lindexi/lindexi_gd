using System;

namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 选择类型
/// </summary>
[Flags]
public enum SelectionType
{
    /// <summary>
    /// 向左按照字符选择
    /// </summary>
    LeftByCharacter = 0B0000_0000_0001_0000_0000,

    /// <summary>
    /// 向右按照字符选择
    /// </summary>
    RightByCharacter = 0B0000_0000_0010_0000_0000,

    /// <summary>
    /// 向左按照词组选择
    /// </summary>
    LeftByWord = 0B0000_0000_0100_0000_0000,

    /// <summary>
    /// 向右按照词组选择
    /// </summary>
    RightByWord = 0B0000_0000_1000_0000_0000,

    /// <summary>
    /// 向上选择段落
    /// </summary>
    UpByLine = 0B0000_0001_0000_0000_0000,

    /// <summary>
    /// 向下选择段落
    /// </summary>
    DownByLine = 0B0000_0010_0000_0000_0000,

    /// <summary>
    /// 选择到段落开始
    /// </summary>
    LineStart = 0B0000_0100_0000_0000_0000,

    /// <summary>
    /// 选择到段落结束
    /// </summary>
    LineEnd = 0B0000_1000_0000_0000_0000,

    /// <summary>
    /// 选择到文档开始
    /// </summary>
    DocumentStart = 0B0001_0000_0000_0000_0000,

    /// <summary>
    /// 选择到文档结束
    /// </summary>
    DocumentEnd = 0B0010_0000_0000_0000_0000,

    /// <summary>
    /// 全选
    /// </summary>
    All = ((int) -1 & (~0B1111_1111)), // 1111111111111_0000_0000

    /// <summary>
    /// Shift左选择
    /// </summary>
    ShiftLeft = DirectionMask.Shift | Direction.Left,

    /// <summary>
    /// Shift右选择
    /// </summary>
    ShiftRight = DirectionMask.Shift | Direction.Right,

    /// <summary>
    /// Shift上选择
    /// </summary>
    ShiftUp = DirectionMask.Shift | Direction.Up,

    /// <summary>
    /// Shift下选择
    /// </summary>
    ShiftDown = DirectionMask.Shift | Direction.Down,

    /// <summary>
    /// Ctrl+Shift左选择
    /// </summary>
    ControlShiftLeft = DirectionMask.Control | DirectionMask.Shift | Direction.Left,

    /// <summary>
    /// Ctrl+Shift右选择
    /// </summary>
    ControlShiftRight = DirectionMask.Control | DirectionMask.Shift | Direction.Right,

    /// <summary>
    /// Ctrl+Shift上选择
    /// </summary>
    ControlShiftUp = DirectionMask.Control | DirectionMask.Shift | Direction.Up,

    /// <summary>
    /// Ctrl+Shift下选择
    /// </summary>
    ControlShiftDown = DirectionMask.Control | DirectionMask.Shift | Direction.Down,
}