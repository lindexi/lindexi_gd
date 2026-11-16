using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeCore;

/// <summary>Describes one legal path and its confirmed registered segments.</summary>
public sealed record ImeInterpretation
(
    ImeInterpretationKind Kind,
    int CoverageEnd,
    ImeInputRange? ConsumedRange,
    string RemainingInput,
    IReadOnlyList<ImeConfirmedSegment> Segments,
    IReadOnlyList<ImePathEvidence> Evidence
);