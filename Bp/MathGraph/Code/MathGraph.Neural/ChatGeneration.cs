using MiniMind.Template;

namespace MathGraphs.Neural;

public sealed partial class MiniMindTextTokenizer
{
    /// <summary>使用训练共享模板渲染基础聊天历史和 assistant 前缀；不截断，openThinking 决定是否保留未闭合思考块。</summary>
    public string RenderChatPrompt(IReadOnlyList<SupervisedChatMessage> messages, bool openThinking = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        cancellationToken.ThrowIfCancellationRequested();
        if (messages.Count == 0 || messages.Count > _maxTextCharacters) throw new ArgumentException(nameof(messages));
        var converted = new List<ChatMessage>();
        long characters = 0;
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (message is null || message.Content is null || message.Role is not ("system" or "user" or "assistant") ||
                (message.Role != "assistant" && message.ReasoningContent is not null))
                throw new ArgumentException(nameof(messages));
            characters += message.Content.Length + (message.ReasoningContent?.Length ?? 0);
            if (characters > _maxTextCharacters) throw new ArgumentOutOfRangeException(nameof(messages));
            converted.Add(new ChatMessage(message.Role, message.Content, message.ReasoningContent));
        }
        string prompt;
        lock (_sync) prompt = _tokenizer.ApplyChatTemplate(converted, true, openThinking);
        cancellationToken.ThrowIfCancellationRequested();
        if (prompt.Length > _maxTextCharacters) throw new ArgumentOutOfRangeException(nameof(messages));
        return prompt;
    }
}

public sealed partial class TokenExecutor
{
    /// <summary>从基础多轮聊天创建新 assistant 回复会话；使用共享模板，不续接历史 assistant 的未完成正文。</summary>
    public TextGenerationSession CreateChatSession(MiniMindTextTokenizer tokenizer,
        IReadOnlyList<SupervisedChatMessage> messages, int maxNewTokens, bool openThinking = false,
        SamplingOptions? options = null, ulong seed = 1, CancellationToken cancellationToken = default)
    {
        ValidateTokenizer(tokenizer);
        ArgumentOutOfRangeException.ThrowIfNegative(maxNewTokens);
        var prompt = tokenizer.RenderChatPrompt(messages, openThinking, cancellationToken);
        return CreateTextSession(tokenizer, prompt, maxNewTokens, options, seed, cancellationToken);
    }
}
