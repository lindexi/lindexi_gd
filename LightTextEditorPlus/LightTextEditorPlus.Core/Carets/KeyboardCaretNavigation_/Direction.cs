using System;

namespace LightTextEditorPlus.Core.Carets;

[Flags]
public enum Direction
{
    None = 0,
    Left = 0B0001,
    Right = 0B0010,
    Up = 0B0100,
    Down = 0B1000,
}