using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

/// <summary>Controls which character case permits shape constraints.</summary>
public enum ImeShapeCasePolicy { UppercaseOnly, UppercaseOrGuess, LowercaseOnly }
/// <summary>Controls legal shape attachment positions.</summary>
public enum ImeShapePlacement { PerCharacter, AfterWord, Both }
/// <summary>Controls complete and pending shape lengths.</summary>
public enum ImeShapeLengthPolicy { OptionalOne, OptionalTwo, OptionalOneOrTwo, RequiredTwo }
/// <summary>Controls deterministic ranking of legal interpretations.</summary>
public enum ImeCandidateSortPolicy { PhoneticFirst, ShapeFirst, Automatic }
/// <summary>Controls automatic selection after typed characters.</summary>
public enum ImeAutoCommitPolicy { Off, Four, Unique }
/// <summary>Identifies bidirectional component equivalences.</summary>
public enum ImeFuzzyRule { ZhZ = 0, ShS = 1, AngAn = 2, EngEn = 3, UanUang = 4, ChC = 5, InIng = 6, IanIang = 7, AiAn = 8, UnOng = 9 }
/// <summary>Identifies completion and selection eligibility.</summary>
public enum ImeInterpretationKind { Complete, Partial, Prefix, IncompleteShape }
/// <summary>Identifies the constraint represented by path evidence.</summary>
public enum ImeEvidenceKind { Phonetic, PerCharacterShape, AfterWordShape, Fuzzy, LookupAlias }
/// <summary>Defines immutable context construction policies.</summary>
public sealed record ImeInputOptions
{
    public required ImeShapeCasePolicy CasePolicy { get; init; }
    public required ImeShapePlacement Placement { get; init; }
    public required ImeShapeLengthPolicy LengthPolicy { get; init; }
    public ImeCandidateSortPolicy SortPolicy { get; init; } = ImeCandidateSortPolicy.PhoneticFirst;
    public ImeAutoCommitPolicy AutoCommit { get; init; }
    public bool EatConfirmationSpace { get; init; }
    public bool EnableLegacyXx { get; init; }
    public bool EnableStandaloneShape { get; init; }
    public IReadOnlyList<ImeFuzzyRule> FuzzyRules { get; init; } = [];
}
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
/// <summary>Identifies a selectable semantic result within a context revision.</summary>
public readonly record struct ImeSelectionId(Guid ContextId, long Revision, int Ordinal);
/// <summary>Records original input and canonical constraint evidence.</summary>
public sealed record ImePathEvidence(ImeEvidenceKind Kind, ImeInputRange Range, int SyllableIndex, string Input, string CanonicalReading, string? ShapeAlias);
/// <summary>Exposes the deterministic candidate ranking key.</summary>
public sealed record ImeCandidateRank(int CompletionPriority, int CoverageEnd, int SourcePriority, int StrategyPriority, int Frequency);
/// <summary>Describes one legal path and its confirmed registered segments.</summary>
public sealed record ImeInterpretation(ImeInterpretationKind Kind, int CoverageEnd, ImeInputRange? ConsumedRange, string RemainingInput, IReadOnlyList<ImeConfirmedSegment> Segments, IReadOnlyList<ImePathEvidence> Evidence);
/// <summary>Groups paths with identical conversion semantics.</summary>
public sealed record ImeConversionCandidate(ImeCandidate Candidate, ImeSelectionId? SelectionId, IReadOnlyList<ImeInterpretation> Interpretations, ImeCandidateRank Rank);
/// <summary>Exposes a final result page without truncating decoding.</summary>
public sealed record ImeConversionPage(IReadOnlyList<ImeConversionCandidate> Candidates, int TotalCount, int Offset, int Limit)
{
    public bool HasMore => Offset < TotalCount && Candidates.Count < TotalCount - Offset;
}
/// <summary>Exposes original character marks and the current interpretation page.</summary>
public sealed record ImeDetailedSnapshot(ImeSessionSnapshot Session, IReadOnlyList<bool> UppercaseMarks, ImeConversionPage Page, long Revision);
