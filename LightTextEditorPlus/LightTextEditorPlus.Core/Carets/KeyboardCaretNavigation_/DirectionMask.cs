namespace LightTextEditorPlus.Core.Carets;

/// <summary>
/// 方向的掩码
/// </summary>
public enum DirectionMask
{
    /// <summary>
    /// 方向的掩码
    /// </summary>
    DirectionMask = Direction.Left | Direction.Right | Direction.Up | Direction.Down, //0B1111

    /// <summary>
    /// 表示 ctrl 键掩码
    /// </summary>
    Control = 0B0000_0000_0001_0000,

    /// <summary>
    /// 表示 shift 键掩码
    /// </summary>
    Shift = 0B0000_0000_0010_0000,
}