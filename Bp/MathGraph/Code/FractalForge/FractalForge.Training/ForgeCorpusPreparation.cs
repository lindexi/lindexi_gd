using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FractalForge.Training;

/// <summary>有界真实语料前缀准备结果；不代表全量语料身份或全量划分审计。</summary>
public sealed record ForgePreparedCorpora(string Semantics, string SourcePath, string PrefixSha256, long SourceBytes,
    int SourceRecords, bool ReachedEnd, ForgeCorpus Training, ForgeCorpus Proposal, ForgeCorpus Acceptance, ForgeCorpus FinalTest);

/// <summary>按解码文本内容稳定划分，重复文本永远不会跨角色；不读取 tokenizer 或训练模型。</summary>
public static class ForgeCorpusPreparation
{
    public const string Semantics = "fractalforge.text-sha256.utf8.exact.mod4.v1";

    /// <summary>有内容角色声明时必须完整匹配用途；旧无声明配置仅保留文件级隔离保证。</summary>
    public static void ValidateUsage(ForgeRunConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var roles = new[] { configuration.Training.ContentRole, configuration.Acceptance.ContentRole, configuration.FinalTest.ContentRole };
        if (roles.Any(r => r.HasValue) && (roles[0] != 0 || roles[1] != 2 || roles[2] != 3)) throw RunErrors.Invalid();
    }

    /// <summary>返回训练、提案、接受、最终测试的固定角色编号；不做语言相关归一化。</summary>
    public static int Role(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return SHA256.HashData(Encoding.UTF8.GetBytes(text))[0] % 4;
    }

    /// <summary>流式读取最多 maxRecords 条原始行并发布新目录；指纹仅覆盖实际消费字节，禁止冒充全量身份。</summary>
    public static async Task<ForgePreparedCorpora> PreparePrefixAsync(string sourcePath, string outputDirectory,
        int maxRecords, int maxLineBytes = 1048576, CancellationToken cancellationToken = default)
    {
        if (maxRecords <= 0 || maxLineBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxRecords));
        var directory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(directory)) throw new IOException(directory);
        Directory.CreateDirectory(directory);
        var names = new[] { "training", "proposal", "acceptance", "final-test" };
        var paths = names.Select(n => Path.Combine(directory, n + ".jsonl")).ToArray();
        var outputs = new List<FileStream>();
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var fingerprint = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var line = new MemoryStream();
        var buffer = new byte[8192];
        long consumed = 0;
        var records = 0;
        var eof = false;
        try
        {
            foreach (var path in paths) outputs.Add(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous));
            while (records < maxRecords)
            {
                var count = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (count == 0) { eof = true; break; }
                var used = 0;
                for (; used < count && records < maxRecords; used++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var value = buffer[used];
                    if (value == 10) { await PublishAsync().ConfigureAwait(false); line.SetLength(0); }
                    else
                    {
                        if (line.Length >= maxLineBytes) throw new InvalidDataException(sourcePath);
                        line.WriteByte(value);
                    }
                }
                fingerprint.AppendData(buffer, 0, used);
                consumed = checked(consumed + used);
            }
            if (eof && line.Length > 0) await PublishAsync().ConfigureAwait(false);
        }
        finally
        {
            foreach (var output in outputs) await output.DisposeAsync().ConfigureAwait(false);
        }
        var prefixHash = Convert.ToHexString(fingerprint.GetHashAndReset());
        var corpora = new List<ForgeCorpus>();
        for (var i = 0; i < paths.Length; i++)
        {
            await using var file = File.OpenRead(paths[i]);
            if (file.Length == 0) throw new InvalidDataException(paths[i]);
            corpora.Add(new(paths[i], Semantics + "/" + prefixHash + "/" + names[i],
                Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken).ConfigureAwait(false)), i));
        }
        var result = new ForgePreparedCorpora(Semantics, Path.GetFullPath(sourcePath), prefixHash, consumed, records, eof,
            corpora[0], corpora[1], corpora[2], corpora[3]);
        await File.WriteAllTextAsync(Path.Combine(directory, "provenance.json"), JsonSerializer.Serialize(result), cancellationToken).ConfigureAwait(false);
        return result;

        async Task PublishAsync()
        {
            var bytes = line.ToArray();
            var offset = records == 0 && bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }) ? 3 : 0;
            using var document = JsonDocument.Parse(bytes.AsMemory(offset));
            var text = document.RootElement.GetProperty("text").GetString() ?? throw new InvalidDataException(sourcePath);
            var canonical = JsonSerializer.SerializeToUtf8Bytes(new { text });
            await outputs[Role(text)].WriteAsync(canonical, cancellationToken).ConfigureAwait(false);
            await outputs[Role(text)].WriteAsync(new byte[] { 10 }, cancellationToken).ConfigureAwait(false);
            records++;
        }
    }
}
