namespace Oxage.Wmf.Primitive;

public struct WmfSize
{
    public WmfSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }
}