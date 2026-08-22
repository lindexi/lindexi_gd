using XiaoXiIme.Foundation;

namespace XiaoXiIme.Dictionary;

public sealed class InMemoryImeDictionary : IImeDictionary
{
    private readonly Dictionary<string, List<ImeCandidate>> _entries;

    public InMemoryImeDictionary(IEnumerable<ImeCandidate> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        _entries = entries
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Reading) && !string.IsNullOrWhiteSpace(candidate.Text))
            .GroupBy(candidate => candidate.Reading, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(candidate => candidate.Score)
                    .ThenBy(candidate => candidate.Text, StringComparer.Ordinal)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public static InMemoryImeDictionary CreateDefault() => new(
    [
        new("你", "ni", 100),
        new("呢", "ni", 60),
        new("好", "hao", 100),
        new("号", "hao", 60),
        new("小希", "xx", 100),
        new("小希", "xiaoxi", 100),
        new("输入法", "shurufa", 100),
    ]);

    public IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9)
    {
        return Query(new ImeDictionaryQuery(reading, maxCount));
    }

    public IReadOnlyList<ImeCandidate> Query(ImeDictionaryQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.Input) || query.MaxCount <= 0)
        {
            return Array.Empty<ImeCandidate>();
        }

        var input = query.Input.Trim();
        if (query.MatchMode == ImeDictionaryMatchMode.Exact)
        {
            return _entries.TryGetValue(input, out var candidates)
                ? candidates.Take(query.MaxCount).ToArray()
                : Array.Empty<ImeCandidate>();
        }

        return _entries
            .Where(entry => entry.Key.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .SelectMany(entry => entry.Value.Select(candidate => new
            {
                Candidate = candidate,
                IsExact = string.Equals(entry.Key, input, StringComparison.OrdinalIgnoreCase),
            }))
            .GroupBy(item => (item.Candidate.Text, item.Candidate.Reading))
            .Select(group => group
                .OrderByDescending(item => item.IsExact)
                .ThenByDescending(item => item.Candidate.Score)
                .First())
            .OrderByDescending(item => item.IsExact)
            .ThenByDescending(item => item.Candidate.Score)
            .ThenBy(item => item.Candidate.Reading.Length)
            .ThenBy(item => item.Candidate.Text, StringComparer.Ordinal)
            .Take(query.MaxCount)
            .Select(item => item.Candidate)
            .ToArray();
    }
}

