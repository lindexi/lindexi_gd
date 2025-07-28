namespace Oxage.Wmf.Primitive;

public struct WmfRectangle
{
    public WmfRectangle(WmfPoint point,WmfSize size)
    {
        Left = point.X;
        Top = point.Y;
        Right = point.X + size.Width;
        Bottom = point.Y + size.Height;
    }

    public WmfRectangle(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }
}