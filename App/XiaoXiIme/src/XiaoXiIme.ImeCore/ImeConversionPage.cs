namespace XiaoXiIme.ImeCore;

/// <summary>Exposes a final result page without truncating decoding.</summary>
public sealed record ImeConversionPage(IReadOnlyList<ImeConversionCandidate> Candidates, int TotalCount, int Offset, int Limit)
{
    public bool HasMore => Offset < TotalCount && Candidates.Count < TotalCount - Offset;
}