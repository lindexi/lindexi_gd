namespace XiaoXiIme.ImeCore;

/// <summary>Exposes the deterministic candidate ranking key.</summary>
public sealed record ImeCandidateRank(int CompletionPriority, int CoverageEnd, int SourcePriority, int StrategyPriority, int Frequency);