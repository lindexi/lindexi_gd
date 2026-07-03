using Microsoft.Extensions.AI;

namespace AgentLib;

/// <summary>
/// 装饰器模式的 <see cref="IChatReducer"/> 包装器，在工具调用尚未完成时跳过压缩。
/// <para>
/// 框架的 <see cref="InMemoryChatHistoryProvider"/> 在
/// <see cref="ChatClientAgentOptions.RequirePerServiceCallChatHistoryPersistence"/> 为 <see langword="true"/> 时，
/// 会在每次服务调用（即每次 <see cref="IChatClient.GetStreamingResponseAsync"/>）后触发
/// <see cref="IChatReducer.ReduceAsync"/>。当模型返回 <see cref="FunctionCallContent"/> 时，
/// 框架会在执行工具 <b>之前</b> 就触发压缩，此时消息中只有工具调用请求但没有工具执行结果，
/// 过早压缩会导致丢失工具上下文或产生不完整的摘要。
/// </para>
/// <para>
/// 此装饰器检测消息中是否存在未配对的 <see cref="FunctionCallContent"/>（即有调用请求但没有对应的
/// <see cref="FunctionResultContent"/>），若存在则原样返回消息、跳过压缩，
/// 等待工具执行完成后再由框架再次触发压缩。
/// </para>
/// </summary>
internal sealed class ToolCallAwareChatReducer : IChatReducer
{
    private readonly IChatReducer _innerReducer;

    /// <summary>
    /// 使用指定的内部压缩器创建装饰器。
    /// </summary>
    /// <param name="innerReducer">被包装的实际压缩器。</param>
    public ToolCallAwareChatReducer(IChatReducer innerReducer)
    {
        ArgumentNullException.ThrowIfNull(innerReducer);
        _innerReducer = innerReducer;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChatMessage>> ReduceAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var input = messages.ToList();

        if (input.Count == 0)
        {
            return input;
        }

        // 如果存在未配对的 FunctionCallContent（有调用请求但无对应结果），
        // 说明工具尚未执行完成，跳过压缩，原样返回。
        if (HasPendingFunctionCalls(input))
        {
            return input;
        }

        return await _innerReducer.ReduceAsync(input, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 检查消息列表中是否存在未配对的 <see cref="FunctionCallContent"/>。
    /// 通过 <see cref="FunctionCallContent.CallId"/> 与 <see cref="FunctionResultContent.CallId"/> 进行配对。
    /// </summary>
    private static bool HasPendingFunctionCalls(List<ChatMessage> messages)
    {
        HashSet<string?> callIds = [];

        foreach (var message in messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    callIds.Add(functionCall.CallId);
                }
            }
        }

        if (callIds.Count == 0)
        {
            return false;
        }

        foreach (var message in messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionResultContent functionResult)
                {
                    callIds.Remove(functionResult.CallId);
                }
            }
        }

        return callIds.Count > 0;
    }
}
