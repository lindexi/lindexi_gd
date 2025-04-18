namespace InkBase;

public readonly record struct StandardRgbColor(byte A, byte R, byte G, byte B)
{
    public static StandardRgbColor FromArgb(byte a, byte r, byte g, byte b)
    {
        return new StandardRgbColor(a, r, g, b);
    }

    public static StandardRgbColor Red => new StandardRgbColor(0xFF, 0xFF, 0x00, 0x00);
}