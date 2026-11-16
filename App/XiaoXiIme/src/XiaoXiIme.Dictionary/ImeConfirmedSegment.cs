namespace XiaoXiIme.Dictionary;

/// <summary>Describes a complete registered segment selected for learning.</summary>
public sealed record ImeConfirmedSegment
    (string Text, string CanonicalReading, int InputStart, int InputLength, int OutputStart, int OutputLength);