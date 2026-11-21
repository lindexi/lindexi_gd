using System.Security.Cryptography;
using System.Text.Json;

namespace MathGraphs.Neural;

public sealed partial class NetworkExecutor
{
    private sealed record StateData(int Format, string Fingerprint, long Step, Dictionary<string, double[]> History);

    /// <summary>保存当前执行快照的循环状态；状态文件与模型检查点配套使用。</summary>
    public byte[] ExportState(RecurrentState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.SnapshotId != _snapshotId) throw new InvalidOperationException(Numeric.Message("StateMismatch"));
        return JsonSerializer.SerializeToUtf8Bytes(new StateData(1, Fingerprint(), state.Step, state.History));
    }

    /// <summary>校验模型快照指纹、节点身份、历史长度和有限值后，将状态显式绑定当前执行器。</summary>
    public RecurrentState ImportState(byte[] bytes, int maxBytes = 67108864)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        if (bytes.Length > maxBytes) throw new InvalidDataException();
        StateData data;
        try
        {
            data = JsonSerializer.Deserialize<StateData>(bytes, new JsonSerializerOptions
            {
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
            }) ?? throw new InvalidDataException();
        }
        catch (JsonException error) { throw new InvalidDataException(null, error); }
        if (data.Format != 1 || data.Step < 0 || data.Fingerprint != Fingerprint() ||
            data.History is null || data.History.Count != _layout.Outputs.Count)
            throw new InvalidDataException();
        foreach (var (id, history) in data.History)
        {
            if (!_layout.Outputs.ContainsKey(id) || history is null || history.Length != _layout.MaxDelay ||
                history.Any(value => !double.IsFinite(value))) throw new InvalidDataException();
            for (var age = 0; age < history.Length; age++)
                if (age >= data.Step && history[age] != 0) throw new InvalidDataException();
        }
        return new RecurrentState(data.History, data.Step, _snapshotId);
    }

    private string Fingerprint() => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        Version,
        _layout.MaxDelay,
        _layout.Operations,
        Values = _layout.Values.OrderBy(p => p.Key).ToArray(),
        Outputs = _layout.Outputs.OrderBy(p => p.Key, StringComparer.Ordinal).ToArray()
    })));
}
