using System;

namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 与键盘移动操作对应的枚举
/// </summary>
[Flags]
public enum CaretMoveType
{
    /// <summary>
    /// 默认值
    /// </summary>
    None,

    /// <summary>
    /// 左
    /// </summary>
    Left = Direction.Left,

    /// <summary>
    /// 右
    /// </summary>
    Right = Direction.Right,

    /// <summary>
    /// 上
    /// </summary>
    Up = Direction.Up,

    /// <summary>
    /// 下
    /// </summary>
    Down = Direction.Down,

    /// <summary>
    /// 行首
    /// </summary>
    LineStart = 0B0000_0000_0001_0000_0000,

    /// <summary>
    /// 行尾
    /// </summary>
    LineEnd = 0B0000_0000_0010_0000_0000,

    /// <summary>
    /// 文档开始
    /// </summary>
    DocumentStart = 0B0000_0000_0100_0000_0000,

    /// <summary>
    /// 文档结束
    /// </summary>
    DocumentEnd = 0B0000_0000_1000_0000_0000,

    /// <summary>
    /// 向下翻页
    /// </summary>
    PageDown = 0B0000_0001_0000_0000_0000,

    /// <summary>
    /// 向上翻页
    /// </summary>
    PageUp = 0B0000_0010_0000_0000_0000,

    /// <summary>
    /// Control左
    /// </summary>
    ControlLeft = Direction.Left | DirectionMask.Control,

    /// <summary>
    /// Control右
    /// </summary>
    ControlRight = Direction.Right | DirectionMask.Control,

    /// <summary>
    /// Control上
    /// </summary>
    ControlUp = Direction.Up | DirectionMask.Control,

    /// <summary>
    /// Control下
    /// </summary>
    ControlDown = Direction.Down | DirectionMask.Control,

    /// <summary>
    /// 左一字符
    /// </summary>
    LeftByCharacter = 0B0000_0100_0000_0000_0000,

    /// <summary>
    /// 右一个字符
    /// </summary>
    RightByCharacter = 0B0000_1000_0000_0000_0000,

    /// <summary>
    /// 左一个词语
    /// </summary>
    LeftByWord = 0B0001_0000_0000_0000_0000,

    /// <summary>
    /// 右一个词语
    /// </summary>
    RightByWord = 0B0010_0000_0000_0000_0000,

    /// <summary>
    /// 上一行
    /// </summary>
    UpByLine = 0B0100_0000_0000_0000_0000,

    /// <summary>
    /// 下一行
    /// </summary>
    DownByLine = 0B1000_0000_0000_0000_0000,
}