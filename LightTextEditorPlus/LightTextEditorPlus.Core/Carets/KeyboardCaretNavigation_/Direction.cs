using System;

namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 方向
/// </summary>
[Flags]
public enum Direction
{
    /// <summary>
    /// 没有方向
    /// </summary>
    None = 0,

    /// <summary>
    /// 左
    /// </summary>
    Left = 0B0001,

    /// <summary>
    /// 右
    /// </summary>
    Right = 0B0010,

    /// <summary>
    /// 上
    /// </summary>
    Up = 0B0100,

    /// <summary>
    /// 下
    /// </summary>
    Down = 0B1000,
}