namespace X11ApplicationFramework.Utils.Edid;

public readonly record struct Cm(uint Value)
{
    public override string ToString() => $"{Value} cm";
}