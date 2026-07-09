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
        new("小希", "xiaoxi", 100),
        new("输入法", "shurufa", 100),
    ]);

    public IReadOnlyList<ImeCandidate> Query(string reading, int maxCount = 9)
    {
        if (string.IsNullOrWhiteSpace(reading) || maxCount <= 0)
        {
            return Array.Empty<ImeCandidate>();
        }

        return _entries.TryGetValue(reading, out var candidates)
            ? candidates.Take(maxCount).ToArray()
            : Array.Empty<ImeCandidate>();
    }
}

