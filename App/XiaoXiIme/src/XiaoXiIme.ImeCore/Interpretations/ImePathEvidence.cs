namespace XiaoXiIme.ImeCore;

/// <summary>Records original input and canonical constraint evidence.</summary>
public sealed record ImePathEvidence
(
    ImeEvidenceKind Kind,
    ImeInputRange Range,
    int SyllableIndex,
    string Input,
    string CanonicalReading,
    string? ShapeAlias
);