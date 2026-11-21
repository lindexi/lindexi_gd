using System.Security.Cryptography;
using System.Text.Json;

namespace MathGraphs.Neural;

public sealed partial class TokenExecutor
{
    private sealed record TokenStateData(int Format, string Fingerprint, byte[] State);

    /// <summary>保存包含文本计算定义指纹的循环会话状态。</summary>
    public byte[] ExportState(RecurrentState state) => JsonSerializer.SerializeToUtf8Bytes(
        new TokenStateData(1, TextFingerprint(), _executor.ExportState(state)));

    /// <summary>校验 tokenizer、编码、K 步和读出后，显式恢复同一模型的文本会话。</summary>
    public RecurrentState ImportState(byte[] bytes, int maxBytes = 67108864)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        if (bytes.Length > maxBytes) throw new InvalidDataException();
        TokenStateData data;
        try
        {
            data = JsonSerializer.Deserialize<TokenStateData>(bytes, new JsonSerializerOptions
            {
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
            }) ?? throw new InvalidDataException();
        }
        catch (JsonException error) { throw new InvalidDataException(null, error); }
        if (data.Format != 1 || data.Fingerprint != TextFingerprint() || data.State is null)
            throw new InvalidDataException();
        return _executor.ImportState(data.State, maxBytes);
    }

    private string TextFingerprint() => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        _bindings.TokenizerIdentity,
        _bindings.InternalSteps,
        _bindings.InputNodes,
        _bindings.ReadoutNodes,
        Encoding = _bindings.CopyEncoding(),
        Weights = _weights,
        Biases = _biases
    })));
}
