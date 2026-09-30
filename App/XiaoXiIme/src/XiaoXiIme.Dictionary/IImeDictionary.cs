using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

public interface IImeDictionary
{
    /// <summary>
    /// Queries ranked candidates for the current input. Implementations rank internally and return
    /// core-facing <see cref="ImeCandidate"/> values.
    /// </summary>
    IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query);

    IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return Query(new ImeDictionaryQuery(reading, maxCount));
    }

    /// <summary>
    /// Queries ranked phonetic readings for an exact candidate text without changing input state.
    /// Implementations keep the original word in <see cref="ImeCandidate.Text"/> and the reading in
    /// <see cref="ImeCandidate.Reading"/>. The default implementation returns no reverse matches.
    /// </summary>
    IReadOnlyList<ImeCandidate> QueryByText(string text, int maxCount = 9) => [];
}