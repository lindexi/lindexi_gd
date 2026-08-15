using AgentLib.Reducers;
using Microsoft.Extensions.AI;

namespace AgentLib;

/// <summary>
/// 针对尾部连续 Assistant+Tool 消息块的 LLM 摘要压缩器。
/// 仅当尾部连续的 Assistant/Tool 消息字符总长度达到阈值时才触发压缩。
/// </summary>
public class CopilotChatManagerToolCallChatReducer : IChatReducer
{
    /// <summary>
    /// 使用指定的聊天客户端和自定义阈值创建压缩器。
    /// </summary>
    /// <param name="chatClient">用于生成摘要的聊天客户端。</param>
    /// <param name="characterThreshold">触发压缩的字符长度阈值。</param>
    /// <param name="compressionObserver">自动压缩观察者。</param>
    public CopilotChatManagerToolCallChatReducer(
        IChatClient chatClient,
        int characterThreshold = DefaultCharacterThreshold,
        ICopilotChatCompressionObserver? compressionObserver = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        if (characterThreshold < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(characterThreshold), "阈值必须大于等于 1。");
        }

        _chatClient = chatClient;
        _compressionObserver = compressionObserver;
        CharacterThreshold = characterThreshold;
    }

    /// <summary>
    /// 默认触发压缩的字符长度阈值。
    /// </summary>
    public const int DefaultCharacterThreshold = 50000;

    private readonly IChatClient _chatClient;
    private readonly ICopilotChatCompressionObserver? _compressionObserver;

    /// <summary>
    /// 触发压缩的字符长度阈值。
    /// </summary>
    public int CharacterThreshold { get; set; }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var input = messages.ToList();

        if (input.Count == 0)
        {
            return input;
        }

        // 从后向前查找尾部连续的 Assistant/Tool 消息块
        int tailStartIndex = FindTailAssistantToolBlockStart(input);

        // 没有找到可压缩块（最后一条是 User/System，或没有 Assistant/Tool 消息）
        if (tailStartIndex >= input.Count)
        {
            return input;
        }

        // 计算可压缩块的字符总长度
        int totalLength = CalculateBlockCharacterLength(input, tailStartIndex, input.Count);

        // 未达到阈值，不压缩
        if (totalLength < CharacterThreshold)
        {
            return input;
        }

        int compressedMessageCount = input.Count - tailStartIndex;
        var statistics = new CopilotChatCompressionStatistics(
            input.Count,
            compressedMessageCount,
            totalLength,
            CharacterThreshold);
        if (_compressionObserver is not null)
        {
            await _compressionObserver
                .CompressionStartedAsync(statistics, cancellationToken)
                .ConfigureAwait(false);
        }

        var messagesToSummarize = new List<ChatMessage>(compressedMessageCount + 2)
        {
            new ChatMessage(ChatRole.System, SummarizationStartPrompt)
        };

        for (int i = tailStartIndex; i < input.Count; i++)
        {
            messagesToSummarize.Add(input[i]);
        }

        messagesToSummarize.Add(new ChatMessage(ChatRole.System, SummarizationEndPrompt));

        try
        {
            ChatResponse chatResponse = await _chatClient
                .GetResponseAsync(messagesToSummarize, cancellationToken: cancellationToken).ConfigureAwait(false);
            var result = new List<ChatMessage>(tailStartIndex + chatResponse.Messages.Count);

            for (int i = 0; i < tailStartIndex; i++)
            {
                result.Add(input[i]);
            }

            result.AddRange(chatResponse.Messages);
            AIContent[] summaryContents = chatResponse.Messages
                .Where(message => message.Role == ChatRole.Assistant)
                .SelectMany(message => message.Contents)
                .ToArray();
            if (_compressionObserver is not null)
            {
                await _compressionObserver
                    .CompressionCompletedAsync(
                        new CopilotChatCompressionResult(statistics, result.Count, summaryContents),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (_compressionObserver is not null)
            {
                await _compressionObserver
                    .CompressionFailedAsync(statistics, exception, CancellationToken.None)
                    .ConfigureAwait(false);
            }

            return input;
        }
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

    /// <summary>
    /// 计算指定范围内消息的字符总长度。
    /// 包括 TextContent.Text、FunctionCallContent 的 Name/Arguments、FunctionResultContent.Result。
    /// </summary>
    private static int CalculateBlockCharacterLength(List<ChatMessage> messages, int startIndex, int endIndex)
    {
        int total = 0;

        for (int i = startIndex; i < endIndex; i++)
        {
            var message = messages[i];

            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        total += textContent.Text?.Length ?? 0;
                        break;
                    case FunctionCallContent functionCallContent:
                        total += functionCallContent.Name?.Length ?? 0;
                        total += functionCallContent.Arguments?.Count ?? 0;
                        break;
                    case FunctionResultContent functionResultContent:
                        total += functionResultContent.Result?.ToString()?.Length ?? 0;
                        break;
                }
            }
        }

        return total;
    }

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
