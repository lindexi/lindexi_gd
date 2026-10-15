using MiniMind.Template;

namespace MathGraphs.Neural;

/// <summary>基础聊天消息；工具调用不属于此数据契约。</summary>
public sealed record SupervisedChatMessage(string Role, string Content, string? ReasoningContent = null);

public sealed partial class MiniMindTextTokenizer
{
    /// <summary>复用 MiniMind 模板构建聊天监督样本；监督 assistant 的思考块、回答和 EOS，不监督角色头及尾随换行。</summary>
    public TokenDataset CreateChatDataset(IEnumerable<IReadOnlyList<SupervisedChatMessage>> conversations,
        int maxSampleTokens = 1024, int maxTotalTokens = 1000000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conversations);
        if (maxSampleTokens < 2 || maxTotalTokens < 2) throw new ArgumentOutOfRangeException(nameof(maxSampleTokens));
        var samples = new List<IReadOnlyList<int>>();
        var masks = new List<IReadOnlyList<bool>>();
        var total = 0;
        foreach (var conversation in conversations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(conversation);
            if (conversation.Count == 0 || conversation.Count > _maxTextCharacters) throw new ArgumentException(nameof(conversations));
            var messages = new List<ChatMessage>();
            var spans = new List<(int Start, int End)>();
            var rendered = string.Empty;
            long characters = 0;
            foreach (var message in conversation)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (message is null || message.Content is null ||
                    message.Role is not ("system" or "user" or "assistant") ||
                    (message.Role != "assistant" && message.ReasoningContent is not null))
                    throw new ArgumentException(nameof(conversations));
                characters += message.Content.Length + (message.ReasoningContent?.Length ?? 0);
                if (characters > _maxTextCharacters) throw new ArgumentOutOfRangeException(nameof(conversations));
                var start = rendered.Length;
                messages.Add(new ChatMessage(message.Role, message.Content, message.ReasoningContent));
                lock (_sync) rendered = _tokenizer.ApplyChatTemplate(messages, false);
                if (rendered.Length > _maxTextCharacters) throw new ArgumentOutOfRangeException(nameof(conversations));
                if (message.Role == "assistant")
                {
                    var header = _tokenizer.BosToken + "assistant\n";
                    if (!rendered.AsSpan(start).StartsWith(header, StringComparison.Ordinal) || !rendered.EndsWith(_tokenizer.EosToken + "\n", StringComparison.Ordinal))
                        throw new InvalidOperationException(Numeric.Message("ChatBoundary"));
                    spans.Add((start + header.Length, rendered.Length - 1));
                }
            }
            // 验证完整编码前缀，而不是假设字符偏移能够直接换算为 token 偏移。
            var tokens = Encode(rendered, int.MaxValue);
            var labels = new bool[tokens.Count];
            foreach (var (start, end) in spans)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var first = Boundary(start);
                var last = Boundary(end);
                Array.Fill(labels, true, first, last - first);
            }
            var length = Math.Min(tokens.Count, maxSampleTokens);
            if (length < 2 || !labels.Take(length).Skip(1).Any(value => value))
                throw new ArgumentException(Numeric.Message("ChatNoLabels"), nameof(conversations));
            if (length > maxTotalTokens - total) throw new ArgumentOutOfRangeException(nameof(maxTotalTokens));
            samples.Add(tokens.Take(length).ToArray());
            masks.Add(labels.Take(length).ToArray());
            total += length;

            int Boundary(int position)
            {
                var prefix = Encode(rendered[..position], int.MaxValue);
                if (prefix.Count > tokens.Count || !prefix.SequenceEqual(tokens.Take(prefix.Count)))
                    throw new InvalidOperationException(Numeric.Message("ChatBoundary"));
                return prefix.Count;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return new TokenDataset(Identity, samples, maxTotalTokens, masks);
    }
}
