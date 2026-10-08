using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

namespace AgentLib.Coding;

internal sealed class ResponsesCodingAgentRunner
{
    public required IManualSendMessageContext MessageContext { get; init; }

    public required IReadOnlyList<AIContent> Contents { get; init; }

    public required CopilotResponsesSession Conversation { get; init; }

    public required ResponseItem Input { get; init; }

    public required string Instructions { get; init; }

    public required ResponsesClient Client { get; init; }

    public required CodingRunWorkspaceContext Workspace { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public ReasoningEffort? ReasoningEffort { get; init; }

    internal async Task<string?> RunAsync()
    {
        var functions = Workspace.Tools.OfType<AIFunction>()
            .ToDictionary(function => function.Name, StringComparer.Ordinal);
        await Task.Yield();
        IDisposable? chatting = null;
        try
        {
            await DispatchAsync(() =>
            {
                InitializeUserMessage();
                chatting = MessageContext.StartChatting();
            }).ConfigureAwait(false);
            await MessageContext.AppendMessagesToSessionAsync().ConfigureAwait(false);
            Conversation.AppendItem(Input);

            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();
                ResponseResult response = await ReceiveResponseAsync(CreateRequest(functions.Values)).ConfigureAwait(false);
                Conversation.AppendResponse(response);
                if (response.Status != ResponseStatus.Completed)
                {
                    throw new InvalidOperationException(response.Error?.Message ?? $"Responses 未完成：{response.Status}");
                }

                var calls = response.OutputItems.OfType<FunctionCallResponseItem>().ToArray();
                if (calls.Length == 0)
                {
                    return response.GetOutputText();
                }
                await ExecuteToolsAsync(calls, functions).ConfigureAwait(false);
            }
        }
        finally
        {
            await DispatchAsync(() =>
            {
                if (MessageContext.AssistantChatMessage.Content == CopilotChatMessage.PlaceholderContent)
                {
                    MessageContext.AssistantChatMessage.ClearMessageItems();
                }
                chatting?.Dispose();
            }).ConfigureAwait(false);
        }
    }

    private void InitializeUserMessage()
    {
        CopilotChatMessage message = MessageContext.UserChatMessage;
        message.ClearMessageItems();
        foreach (AIContent content in Contents)
        {
            if (content is TextContent text)
            {
                message.AppendText(text.Text);
            }
            else if (content is DataContent image)
            {
                message.MessageItems.Add(new CopilotChatImageItem(BinaryData.FromBytes(image.Data), image.MediaType));
            }
        }
    }

    private CreateResponseOptions CreateRequest(IEnumerable<AIFunction> functions)
    {
        var request = new CreateResponseOptions
        {
            Model = MessageContext.LanguageModel.ModelDefinition.ModelId,
            Instructions = Instructions,
            StreamingEnabled = true,
            StoredOutputEnabled = true,
            ReasoningOptions = ReasoningEffort is { } effort
                ? new ResponseReasoningOptions { ReasoningEffortLevel = new ResponseReasoningEffortLevel(effort.ToString().ToLowerInvariant()) }
                : null,
        };
        foreach (ResponseItem item in Conversation.Items)
        {
            request.InputItems.Add(item);
        }
        foreach (AIFunction function in functions)
        {
            request.Tools.Add(ResponsesToolHelper.CreateTool(function));
        }
        request.Tools.Add(ResponseTool.CreateWebSearchTool());
        return request;
    }

    private async Task<ResponseResult> ReceiveResponseAsync(CreateResponseOptions request)
    {
        ResponseResult? response = null;
        await foreach (StreamingResponseUpdate update in Client
            .CreateResponseStreamingAsync(request, CancellationToken).ConfigureAwait(false))
        {
            await DispatchAsync(() => MessageContext.AssistantChatMessage.ResponseInfo
                .AppendResponseUpdate(update)).ConfigureAwait(false);
            switch (update)
            {
                case StreamingResponseCompletedUpdate completed:
                    response = completed.Response;
                    break;
                case StreamingResponseIncompleteUpdate incomplete:
                    response = incomplete.Response;
                    break;
                case StreamingResponseFailedUpdate failed:
                    response = failed.Response;
                    break;
                case StreamingResponseErrorUpdate error:
                    throw new InvalidOperationException(error.Message);
            }
        }
        return response ?? throw new InvalidOperationException("Responses 流未返回终态响应。");
    }

    private async Task ExecuteToolsAsync(IReadOnlyList<FunctionCallResponseItem> calls,
        IReadOnlyDictionary<string, AIFunction> functions)
    {
        foreach (FunctionCallResponseItem call in calls)
        {
            CancellationToken.ThrowIfCancellationRequested();
            FunctionCallOutputResponseItem output = await ResponsesToolHelper
                .InvokeAsync(functions[call.FunctionName], call, CancellationToken).ConfigureAwait(false);
            Conversation.AppendItem(output);
            await DispatchAsync(() => MessageContext.AssistantChatMessage.ResponseInfo
                .AppendToolResult(output)).ConfigureAwait(false);
        }
    }

    private Task DispatchAsync(Action action)
    {
        if (MessageContext.MainThreadDispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            return dispatcher.InvokeAsync(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }
        action();
        return Task.CompletedTask;
    }
}
