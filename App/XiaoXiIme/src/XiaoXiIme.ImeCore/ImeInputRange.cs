namespace XiaoXiIme.ImeCore;

/// <summary>Describes a checked UTF-16 half-open input interval.</summary>
public readonly record struct ImeInputRange
{
    /// <summary>Creates a nonnegative interval with a representable endpoint.</summary>
    public ImeInputRange(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (start > int.MaxValue - length) throw new ArgumentOutOfRangeException(nameof(length));
        Start = start; Length = length;
    }
    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;
}