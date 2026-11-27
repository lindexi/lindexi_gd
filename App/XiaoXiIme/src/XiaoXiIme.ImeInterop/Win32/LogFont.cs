namespace XiaoXiIme.ImeInterop;

public unsafe struct LogFont
{
    public int Height;
    public int Width;
    public int Escapement;
    public int Orientation;
    public int Weight;
    public byte Italic;
    public byte Underline;
    public byte StrikeOut;
    public byte CharSet;
    public byte OutPrecision;
    public byte ClipPrecision;
    public byte Quality;
    public byte PitchAndFamily;
    public fixed char FaceName[32];
}