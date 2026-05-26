using Microsoft.Extensions.AI;

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AgentLib.Tests.Fakes;

internal sealed class FakeChatClient : IChatClient
{
    private readonly Queue<Func<IReadOnlyList<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>> _streamHandlers = [];
    private readonly List<FakeChatClientRequest> _requests = [];

    public IReadOnlyList<FakeChatClientRequest> Requests => _requests;

    public void EnqueueStreamingResponse(params ChatResponseUpdate[] updates)
    {
        ArgumentNullException.ThrowIfNull(updates);
        EnqueueStreamingResponse((_, _, cancellationToken) => CreateAsyncEnumerable(updates, cancellationToken));
    }

    public void EnqueueStreamingResponse(Func<IReadOnlyList<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> streamHandler)
    {
        ArgumentNullException.ThrowIfNull(streamHandler);
        _streamHandlers.Enqueue(streamHandler);
    }

    public void EnqueueStreamingResponseWithToolInvocation(string callId, string toolName,
        IDictionary<string, object?>? arguments, params ChatResponseUpdate[] trailingUpdates)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        EnqueueStreamingResponse((messages, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(messages, options, callId, toolName, arguments, trailingUpdates, cancellationToken));
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        List<AIContent> contents = [];
        ChatRole? role = null;

        await foreach (ChatResponseUpdate update in GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
        {
            role = update.Role ?? role;
            if (update.Contents.Count > 0)
            {
                contents.AddRange(update.Contents);
            }
            else if (!string.IsNullOrEmpty(update.Text))
            {
                contents.Add(new TextContent(update.Text));
            }
        }

        ChatMessage responseMessage = contents.Count > 0
            ? new ChatMessage(role ?? ChatRole.Assistant, contents)
            : new ChatMessage(role ?? ChatRole.Assistant, string.Empty);
        return new ChatResponse(responseMessage);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (_streamHandlers.Count == 0)
        {
            throw new InvalidOperationException("未为 FakeChatClient 配置流式响应。");
        }

        IReadOnlyList<ChatMessage> messageList = messages.ToList().AsReadOnly();
        _requests.Add(new FakeChatClientRequest(messageList, options));
        Func<IReadOnlyList<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> streamHandler = _streamHandlers.Dequeue();
        return streamHandler(messageList, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? serviceKey)
    {
        return null;
    }

    public void Dispose()
    {
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateAsyncEnumerable(IEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (ChatResponseUpdate update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolInvocationAsyncEnumerable(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        string callId,
        string toolName,
        IDictionary<string, object?>? arguments,
        IEnumerable<ChatResponseUpdate> trailingUpdates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent(callId, toolName, arguments)]);

        AITool tool = options?.Tools?.FirstOrDefault(candidate => string.Equals(candidate.Name, toolName, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException($"未找到名为 {toolName} 的工具。");

        if (tool is not AIFunction function)
        {
            throw new InvalidOperationException($"工具 {toolName} 不是可调用函数。");
        }

        object? result = await function.InvokeAsync(new AIFunctionArguments(arguments?.ToDictionary(pair => pair.Key, pair => pair.Value)), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionResultContent(callId, NormalizeResult(result))]);

        await foreach (ChatResponseUpdate update in CreateAsyncEnumerable(trailingUpdates, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    private static object? NormalizeResult(object? result)
    {
        if (result is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                _ => jsonElement.ToString()
            };
        }

        return result;
    }
}

internal sealed record FakeChatClientRequest(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options);
