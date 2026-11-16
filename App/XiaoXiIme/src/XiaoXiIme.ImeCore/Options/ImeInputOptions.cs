namespace XiaoXiIme.ImeCore;

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