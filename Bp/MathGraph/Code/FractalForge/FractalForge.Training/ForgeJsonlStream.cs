using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using MathGraphs.Neural;

namespace FractalForge.Training;

/// <summary>逐行校验的完整样本流；恢复时重放前缀预算，不保留整集 token。</summary>
public static class ForgeJsonlStream
{
    /// <summary>验证原始文件字节身份与文件预算。</summary>
    public static async Task VerifyAsync(ForgeCorpus corpus, long maxBytes, CancellationToken ct)
    {
        await using var stream = Open(corpus.Path);
        if (stream.Length <= 0 || stream.Length > maxBytes) throw RunErrors.Invalid();
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
        if (hash != corpus.Sha256) throw RunErrors.Invalid();
    }

    private static FileStream Open(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.Read,
        8192, FileOptions.Asynchronous | FileOptions.SequentialScan);

    /// <summary>按完整样本位置恢复；哈希验证在首次样本发布之前完成。</summary>
    public static async IAsyncEnumerable<IReadOnlyList<int>> ReadAsync(ForgeCorpus corpus, MiniMindTextTokenizer tokenizer,
        JsonlDatasetOptions limits, long skip, [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentNullException.ThrowIfNull(tokenizer);
        ArgumentNullException.ThrowIfNull(limits);
        if (skip < 0) throw RunErrors.Invalid();
        await using var stream = Open(corpus.Path);
        if (stream.Length <= 0 || stream.Length > limits.MaxFileBytes) throw RunErrors.Invalid();
        if (Convert.ToHexString(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false)) != corpus.Sha256) throw RunErrors.Invalid();
        stream.Position = 0;
        using var line = new MemoryStream();
        var buffer = new byte[8192];
        long bytes = 0, samples = 0, tokens = 0;
        var first = true;
        while (true)
        {
            var count = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (count == 0) break;
            bytes = checked(bytes + count);
            if (bytes > limits.MaxFileBytes) throw RunErrors.Invalid();
            for (var i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (buffer[i] != (byte)'\n')
                {
                    if (line.Length >= limits.MaxLineBytes) throw RunErrors.Invalid();
                    line.WriteByte(buffer[i]);
                    continue;
                }
                var sample = await ParseAsync().ConfigureAwait(false);
                if (sample is not null && samples > skip) yield return sample;
                line.SetLength(0);
            }
        }
        if (line.Length > 0)
        {
            var sample = await ParseAsync().ConfigureAwait(false);
            if (sample is not null && samples > skip) yield return sample;
        }
        if (samples == 0 || skip > samples) throw RunErrors.Invalid();

        async Task<IReadOnlyList<int>?> ParseAsync()
        {
            var raw = line.ToArray();
            var offset = first && raw.AsSpan().StartsWith(new byte[] { 239, 187, 191 }) ? 3 : 0;
            first = false;
            if (raw.Skip(offset).All(b => b is 9 or 13 or 32)) return null;
            if (corpus.ContentRole is { } role)
            {
                using var document = System.Text.Json.JsonDocument.Parse(raw.AsMemory(offset));
                var text = document.RootElement.GetProperty("text").GetString() ?? throw RunErrors.Invalid();
                if (role is < 0 or > 3 || ForgeCorpusPreparation.Role(text) != role) throw RunErrors.Invalid();
            }
            using var single = new MemoryStream(raw, offset, raw.Length - offset, false);
            var dataset = await PretrainingJsonl.LoadAsync(single, tokenizer, limits with { MaxSamples = 1 }, ct).ConfigureAwait(false);
            var sample = dataset[0];
            samples++;
            tokens = checked(tokens + sample.Count);
            if (samples > limits.MaxSamples || tokens > limits.MaxTotalTokens) throw RunErrors.Invalid();
            return sample;
        }
    }
}
