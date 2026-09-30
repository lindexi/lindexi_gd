using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

/// <summary>
/// Provides symbol candidates through a query path independent from phonetic frequencies.
/// </summary>
public interface ISymbolDictionary
{
    /// <summary>
    /// Queries the candidates configured for an exact symbol input.
    /// </summary>
    IReadOnlyList<ImeCandidate> QuerySymbols(string input, int maxCount = 9);
}
