using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeCore;

/// <summary>Groups paths with identical conversion semantics.</summary>
public sealed record ImeConversionCandidate(ImeCandidate Candidate, ImeSelectionId? SelectionId, IReadOnlyList<ImeInterpretation> Interpretations, ImeCandidateRank Rank);