namespace XiaoXiIme.Foundation;

/// <summary>Ordered rectangle in target client coordinates, including zero and negative origins.</summary>
public sealed record ImeAnchorRectangle(int Left, int Top, int Right, int Bottom);