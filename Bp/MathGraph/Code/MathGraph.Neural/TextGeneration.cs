namespace MathGraphs.Neural;

/// <summary>绑定已校验词表的文本会话；每步返回 token，文本由完整生成序列解码。</summary>
public sealed class TextGenerationSession
{
    private readonly GenerationSession _session;
    private readonly MiniMindTextTokenizer _tokenizer;

    internal TextGenerationSession(GenerationSession session, MiniMindTextTokenizer tokenizer)
    {
        _session = session;
        _tokenizer = tokenizer;
    }

    public bool IsCompleted => _session.IsCompleted;

    /// <summary>按既定采样规则生成一个 token；取消和失败沿用底层事务语义。</summary>
    public int NextToken(CancellationToken cancellationToken = default) => _session.NextToken(cancellationToken);

    /// <summary>解码全部续写，不包含提示词；未完成的 UTF-8 字节片段可能暂时显示替代字符。</summary>
    public string GetGeneratedText(bool skipSpecialTokens = true) =>
        _tokenizer.Decode(_session.GetGeneratedTokens(), skipSpecialTokens);

    /// <summary>导出可由兼容模型及 tokenizer 恢复的生成状态。</summary>
    public byte[] ExportCheckpoint() => _session.ExportCheckpoint();
}

public sealed partial class TokenExecutor
{
    /// <summary>从原始文本创建生成会话，不添加聊天模板或 BOS；提示词超预算时拒绝而不截断。</summary>
    public TextGenerationSession CreateTextSession(MiniMindTextTokenizer tokenizer, string prompt, int maxNewTokens,
        SamplingOptions? options = null, ulong seed = 1, CancellationToken cancellationToken = default)
    {
        ValidateTokenizer(tokenizer);
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentOutOfRangeException.ThrowIfNegative(maxNewTokens);
        cancellationToken.ThrowIfCancellationRequested();
        if (maxNewTokens >= _budget.MaxSteps) throw new ArgumentOutOfRangeException(nameof(maxNewTokens));
        var limit = Math.Min(_budget.MaxSteps - maxNewTokens, _budget.MaxSteps / _bindings.InternalSteps);
        var tokens = tokenizer.Encode(prompt, checked(limit + 1));
        if (tokens.Count == 0 || tokens.Count > limit) throw new ArgumentOutOfRangeException(nameof(prompt));
        var session = CreateGenerationSession(tokens, maxNewTokens, options, seed, tokenizer.EosTokenId, cancellationToken);
        return new TextGenerationSession(session, tokenizer);
    }

    /// <summary>先校验 tokenizer 身份和词表，再恢复包含随机状态的文本生成会话。</summary>
    public TextGenerationSession RestoreTextSession(MiniMindTextTokenizer tokenizer, byte[] bytes, int maxBytes = 67108864)
    {
        ValidateTokenizer(tokenizer);
        return new TextGenerationSession(RestoreGenerationSession(bytes, maxBytes), tokenizer);
    }

    private void ValidateTokenizer(MiniMindTextTokenizer tokenizer)
    {
        ArgumentNullException.ThrowIfNull(tokenizer);
        if (tokenizer.Identity != _bindings.TokenizerIdentity || tokenizer.VocabularySize != _bindings.VocabularySize)
            throw new ArgumentException(nameof(tokenizer));
    }
}
