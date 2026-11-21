using System.Security.Cryptography;
using System.Text;
using MiniMind.Template;
using MiniMind.Tokenizer;

namespace MathGraphs.Neural;

/// <summary>复用仓库 MiniMind 字节级 BPE 的文本适配器，不包含固定层网络。</summary>
public sealed partial class MiniMindTextTokenizer
{
    private readonly object _sync = new();
    private readonly IMiniMindTokenizer _tokenizer;
    private readonly MiniMindTokenizer _implementation;
    private readonly int _maxTextCharacters;

    private MiniMindTextTokenizer(MiniMindTokenizer tokenizer, string identity, int maxTextCharacters)
    {
        _tokenizer = tokenizer;
        _implementation = tokenizer;
        Identity = identity;
        _maxTextCharacters = maxTextCharacters;
    }

    public string Identity { get; }
    public int CachedEntryCount { get { lock (_sync) return _implementation.CachedEntryCount; } }
    public long CachedCharacterCount { get { lock (_sync) return _implementation.CachedCharacterCount; } }
    public int VocabularySize => _tokenizer.VocabularySize;
    public int BosTokenId => _tokenizer.BosTokenId;
    public int EosTokenId => _tokenizer.EosTokenId;
    public int PadTokenId => _tokenizer.PadTokenId;

    /// <summary>有界异步读取词表与配置，身份包含两份原始文件内容；不支持任意 HuggingFace 模型格式。</summary>
    public static async Task<MiniMindTextTokenizer> LoadAsync(string tokenizerPath, string configPath,
        int maxFileBytes = 16777216, int maxTextCharacters = 32768, CancellationToken cancellationToken = default,
        int maxCacheEntries = 4096, int maxCacheCharacters = 1048576)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenizerPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFileBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTextCharacters);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCacheEntries);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCacheCharacters);
        var tokenizerBytes = await ReadAsync(tokenizerPath).ConfigureAwait(false);
        var configBytes = await ReadAsync(configPath).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var encoding = new UTF8Encoding(false, true);
        var config = TokenizerTemplateConfig.Parse(encoding.GetString(configBytes));
        var tokenizer = MiniMindTokenizer.FromJson(encoding.GetString(tokenizerBytes), config, new MiniMindChatTemplateRenderer(config), maxCacheEntries, maxCacheCharacters);
        var hashInput = SHA256.HashData(tokenizerBytes).Concat(SHA256.HashData(configBytes)).ToArray();
        var identity = "minimind-bpe-v1:" + Convert.ToHexString(SHA256.HashData(hashInput));
        cancellationToken.ThrowIfCancellationRequested();
        return new MiniMindTextTokenizer(tokenizer, identity, maxTextCharacters);

        async Task<byte[]> ReadAsync(string path)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length <= 0 || stream.Length > maxFileBytes) throw new InvalidDataException();
            var bytes = new byte[(int)stream.Length];
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            if (await stream.ReadAsync(new byte[1], cancellationToken).ConfigureAwait(false) != 0) throw new InvalidDataException();
            return bytes;
        }
    }

    /// <summary>编码文本；超字符预算拒绝，maxTokens 明确截断输出，不自动添加 BOS/EOS。</summary>
    public IReadOnlyList<int> Encode(string text, int maxTokens = 1024)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(maxTokens);
        if (text.Length > _maxTextCharacters) throw new ArgumentOutOfRangeException(nameof(text));
        lock (_sync) return Array.AsReadOnly(_tokenizer.Encode(text, maxTokens).ToArray());
    }

    /// <summary>解码 token 序列，可显式跳过特殊 token。</summary>
    public string Decode(IReadOnlyList<int> tokens, bool skipSpecialTokens = false)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (tokens.Count > _maxTextCharacters) throw new ArgumentOutOfRangeException(nameof(tokens));
        lock (_sync) return _tokenizer.Decode(tokens, skipSpecialTokens);
    }

    /// <summary>将独立文本转换为有界预训练样本，显式在末尾追加 EOS，保留相同 tokenizer 身份。</summary>
    public TokenDataset CreateDataset(IEnumerable<string> texts, int maxSampleTokens = 1024,
        int maxTotalTokens = 1000000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (maxSampleTokens < 2) throw new ArgumentOutOfRangeException(nameof(maxSampleTokens));
        return new TokenDataset(Identity, Samples(), maxTotalTokens);

        IEnumerable<IReadOnlyList<int>> Samples()
        {
            foreach (var text in texts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var tokens = Encode(text, maxSampleTokens - 1);
                if (tokens.Count == 0) throw new ArgumentException(nameof(texts));
                yield return tokens.Append(EosTokenId).ToArray();
            }
        }
    }
}
