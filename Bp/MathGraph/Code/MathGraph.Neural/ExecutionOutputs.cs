using System.Collections;

namespace MathGraphs.Neural;

internal sealed class ExecutionOutputs(IReadOnlyDictionary<string, int> indices, double[] values)
    : IReadOnlyDictionary<string, double>
{
    public int Count => indices.Count;
    public IEnumerable<string> Keys => indices.Keys;
    public IEnumerable<double> Values => indices.Values.Select(index => values[index]);
    public double this[string key] => values[indices[key]];

    public bool ContainsKey(string key) => indices.ContainsKey(key);

    public bool TryGetValue(string key, out double value)
    {
        if (indices.TryGetValue(key, out var index))
        {
            value = values[index];
            return true;
        }
        value = default;
        return false;
    }

    public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
    {
        foreach (var (id, index) in indices)
            yield return new KeyValuePair<string, double>(id, values[index]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
