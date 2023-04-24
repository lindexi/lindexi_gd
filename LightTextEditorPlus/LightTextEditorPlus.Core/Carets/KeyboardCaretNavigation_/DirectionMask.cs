namespace LightTextEditorPlus.Core.Carets;

public enum DirectionMask
{
    DirectionMask = Direction.Left | Direction.Right | Direction.Up | Direction.Down,//0B1111
    Control = 0B0000_0000_0001_0000,
    Shift = 0B0000_0000_0010_0000,
}