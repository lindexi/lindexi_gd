using Microsoft.Extensions.AI;

namespace AgentLib.Reducers;

/// <summary>
/// 针对尾部连续 Assistant+Tool 消息块的 LLM 摘要压缩器。
/// 优先使用 LLM 返回的用量统计判断对话长度，并以字符长度兼容缺少用量统计的消息。
/// </summary>
public class CopilotChatManagerToolCallChatReducer : IChatReducer
{
    /// <summary>
    /// 使用指定的聊天客户端创建压缩器。
    /// </summary>
    /// <param name="chatClient">用于生成摘要的聊天客户端。</param>
    public CopilotChatManagerToolCallChatReducer(IChatClient chatClient)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        _chatClient = chatClient;
    }

    /// <summary>
    /// 默认的条件压缩 token 数阈值。
    /// </summary>
    public const int DefaultConditionalCompressionTokenCountThreshold = 50000;

    /// <summary>
    /// 默认的强制压缩 token 数阈值。
    /// </summary>
    public const int DefaultForcedCompressionTokenCountThreshold = 200_000;

    private const int MinimumLastAssistantMessageTokenCount = 10_000;

    private readonly IChatClient _chatClient;

    /// <summary>
    /// 在开始压缩对话时发生。
    /// </summary>
    public event EventHandler? CompressionStarted;

    /// <summary>
    /// 在压缩对话完成时发生。
    /// </summary>
    public event EventHandler<IReadOnlyList<ChatMessage>>? CompressionCompleted;

    /// <summary>
    /// 在压缩对话失败时发生。
    /// </summary>
    public event EventHandler<Exception>? CompressionFailed;

    /// <summary>
    /// 满足消息角色和末条 Assistant 上下文 token 数条件时，触发压缩的 token 数阈值。
    /// 优先采用模型返回的 token 用量，缺少用量时采用消息内容字符数兼容估算。
    /// </summary>
    public int ConditionalCompressionTokenCountThreshold { get; init; }
        = DefaultConditionalCompressionTokenCountThreshold;

    /// <summary>
    /// 忽略消息角色和末条 Assistant 上下文 token 数条件，直接触发压缩的 token 数阈值。
    /// 优先采用模型返回的 token 用量，缺少用量时采用消息内容字符数兼容估算。
    /// </summary>
    public int ForcedCompressionTokenCountThreshold { get; init; }
        = DefaultForcedCompressionTokenCountThreshold;

    /// <inheritdoc/>
    public async Task<IEnumerable<ChatMessage>> ReduceAsync
        (IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var input = messages.ToList();
        int totalTokenCount = CalculateCurrentContextTokenCount(input);
        bool forceCompression = totalTokenCount >= ForcedCompressionTokenCountThreshold;
        if (!ShouldCompress(input, totalTokenCount))
        {
            return input;
        }

        int tailStartIndex = FindTailAssistantToolBlockStart(input);
        if (tailStartIndex >= input.Count)
        {
            if (!forceCompression)
            {
                return input;
            }

            tailStartIndex = 0;
        }

        // 构建压缩请求：保留非压缩部分 + 插入起点提示词 + 压缩块内容 + 插入末尾提示词
        var messagesToSummarize = new List<ChatMessage>(tailStartIndex + 2)
        {
            new ChatMessage(ChatRole.System, SummarizationStartPrompt)
        };

        for (int i = tailStartIndex; i < input.Count; i++)
        {
            messagesToSummarize.Add(input[i]);
        }

        messagesToSummarize.Add(new ChatMessage(ChatRole.System, SummarizationEndPrompt));

        CompressionStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            // 调用 LLM 生成摘要
            var chatResponse = await _chatClient
                .GetResponseAsync(messagesToSummarize, cancellationToken: cancellationToken).ConfigureAwait(false);
            CompressionCompleted?.Invoke(this, (IReadOnlyList<ChatMessage>)chatResponse.Messages);

            // 构建结果：保留非压缩部分 + 摘要消息
            var result = new List<ChatMessage>(tailStartIndex + 1);

            for (int i = 0; i < tailStartIndex; i++)
            {
                result.Add(input[i]);
            }

            // 将 LLM 返回的摘要消息加入结果
            result.AddRange(chatResponse.Messages);

            return result;
        }
        catch (Exception exception)
        {
            CompressionFailed?.Invoke(this, exception);
            // 压缩失败，就不影响了
            return input;
        }
    }

    private bool ShouldCompress(List<ChatMessage> messages, int totalTokenCount)
    {
        if (totalTokenCount >= ForcedCompressionTokenCountThreshold)
        {
            return true;
        }

        if (messages.Count == 0 || totalTokenCount < ConditionalCompressionTokenCountThreshold)
        {
            return false;
        }

        ChatMessage lastMessage = messages[^1];
        if (lastMessage.Role == ChatRole.User || lastMessage.Role == ChatRole.System)
        {
            return false;
        }

        return lastMessage.Role != ChatRole.Assistant
               || CalculateAssistantOutputTokenCount(lastMessage) >= MinimumLastAssistantMessageTokenCount;
    }

    /// <summary>
    /// 从消息列表末尾向前查找，返回尾部连续 Assistant/Tool 块的起始索引。
    /// 若最后一条消息不是 Assistant/Tool，返回消息总数（表示无可压缩块）。
    /// </summary>
    private static int FindTailAssistantToolBlockStart(List<ChatMessage> messages)
    {
        int i = messages.Count - 1;

        while (i >= 0)
        {
            var role = messages[i].Role;
            if (role == ChatRole.Assistant || role == ChatRole.Tool)
            {
                i--;
            }
            else
            {
                break;
            }
        }

        return i + 1;
    }

    private static int CalculateCurrentContextTokenCount(List<ChatMessage> messages)
    {
        long total = 0;

        for (int messageIndex = messages.Count - 1; messageIndex >= 0; messageIndex--)
        {
            IList<AIContent> contents = messages[messageIndex].Contents;
            for (int contentIndex = contents.Count - 1; contentIndex >= 0; contentIndex--)
            {
                AIContent content = contents[contentIndex];
                if (content is UsageContent { Details.TotalTokenCount: > 0 } usageContent)
                {
                    return ClampTokenCount(total + usageContent.Details.TotalTokenCount.Value);
                }

                total += EstimateContentTokenCount(content);
            }
        }

        return ClampTokenCount(total);
    }

    private static int CalculateAssistantOutputTokenCount(ChatMessage message)
    {
        long total = 0;
        IList<AIContent> contents = message.Contents;

        for (int contentIndex = contents.Count - 1; contentIndex >= 0; contentIndex--)
        {
            AIContent content = contents[contentIndex];
            if (content is UsageContent { Details.OutputTokenCount: > 0 } usageContent)
            {
                return ClampTokenCount(total + usageContent.Details.OutputTokenCount.Value);
            }

            total += EstimateContentTokenCount(content);
        }

        return ClampTokenCount(total);
    }

    private static int EstimateContentTokenCount(AIContent content)
    {
        return content switch
        {
            TextContent textContent => textContent.Text?.Length ?? 0,
            FunctionCallContent functionCallContent =>
                (functionCallContent.Name?.Length ?? 0) + (functionCallContent.Arguments?.Count ?? 0),
            FunctionResultContent functionResultContent => functionResultContent.Result?.ToString()?.Length ?? 0,
            _ => 0,
        };
    }

    private static int ClampTokenCount(long tokenCount)
        => tokenCount >= int.MaxValue ? int.MaxValue : (int)tokenCount;

    /// <summary>
    /// 压缩起点系统提示词，告知 LLM 角色和任务。
    /// </summary>
    private const string SummarizationStartPrompt
        = "你是一个总结助手。请将以下 Assistant 调用工具以及工具返回结果的对话进行总结。只做总结，不要回答任何问题。";

    /// <summary>
    /// 压缩末尾系统提示词，要求总结已完成的事情、做出的决策、当前状态、后续计划和思路。
    /// </summary>
    private const string SummarizationEndPrompt
        = """
          请总结以上对话内容。只需要输出总结，不要回答任何问题。

          总结必须包含以下五个方面：
          - 已做了什么：Assistant 实际完成了哪些操作、获取了哪些信息，用自然语言描述做了什么，而不是罗列调用了什么工具。
          - 做出了什么决策：Assistant 在过程中做了哪些关键判断或选择，为什么这样选择。
          - 当前状态：最终得到了什么结论、数据或产出，当前处于什么阶段。
          - 后续计划：接下来准备做什么，还需要哪些信息或操作才能完成任务。
          - 思路：整体逻辑链条是什么，为什么按这样的顺序推进。

          确保总结能帮助后续对话理解上下文并继续完成任务。
          """;
}