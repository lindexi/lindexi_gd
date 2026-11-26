using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AgentLib;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;

#pragma warning disable OPENAI001

namespace PptxGenerator.Pipeline;

internal sealed class SlideResponsesClient(CopilotChatManager copilotChatManager)
{
    private readonly CopilotChatManager _copilotChatManager = copilotChatManager;
    private readonly ConditionalWeakTable<CopilotChatMessage, ResponseItem[]> _turnStarts = new();

    internal async Task<string> SendAsync(
        IReadOnlyList<AIContent> contents, string? instructions, IReadOnlyList<AIFunction> tools,
        CancellationToken externalCancellationToken, bool withHistory = true, bool isPresetInfo = false,
        Func<string, Task>? onTextDelta = null, CancellationToken? streamCancellationToken = null)
    {
        var session = _copilotChatManager.SelectedSession;
        var manualContext = await _copilotChatManager
            .CreateManualSendMessageContextAsync(externalCancellationToken);
        var client = await GetClientAsync(manualContext.LanguageModel);
        var conversation = withHistory ? session.ResponsesSession : new CopilotResponsesSession();
        var input = ResponseItem.CreateUserMessageItem(contents.Select(CreateInputPart));
        var cancellationToken = streamCancellationToken ?? externalCancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        manualContext.UserChatMessage.IsPresetInfo = isPresetInfo;
        manualContext.AssistantChatMessage.IsPresetInfo = isPresetInfo;
        foreach (var content in contents)
        {
            if (content is TextContent text)
                manualContext.UserChatMessage.AppendText(text.Text);
            else if (content is DataContent image)
                manualContext.UserChatMessage.MessageItems.Add(new CopilotChatImageItem(BinaryData.FromBytes(image.Data), image.MediaType));
        }

        await manualContext.AppendMessagesToSessionAsync();
        using var chatting = manualContext.StartChatting();
        if (withHistory)
            _turnStarts.Add(manualContext.UserChatMessage, conversation.Items.ToArray());
        var persistInstructions = withHistory && conversation.Items.Count == 0 && !string.IsNullOrWhiteSpace(instructions);
        if (persistInstructions)
            conversation.AppendItem(ResponseItem.CreateDeveloperMessageItem(instructions));
        conversation.AppendItem(input);

        var partialText = new StringBuilder();
        var assistant = manualContext.AssistantChatMessage;
        try
        {
            const int maxResponses = 32;
            for (var round = 0; round < maxResponses; round++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var request = new CreateResponseOptions
                {
                    Model = client.ModelId,
                    Instructions = persistInstructions ? null : instructions,
                    StreamingEnabled = true,
                    StoredOutputEnabled = false,
                };
                foreach (var item in conversation.Items)
                    request.InputItems.Add(item);
                foreach (var tool in tools)
                    request.Tools.Add(ResponsesToolHelper.CreateTool(tool));
                request.IncludedProperties.Add(IncludedResponseProperty.ReasoningEncryptedContent);

                ResponseResult? completed = null;
                await foreach (var update in client.Client.CreateResponseStreamingAsync(request, cancellationToken))
                {
                    assistant.ResponseInfo.AppendResponseUpdate(update);
                    switch (update)
                    {
                        case StreamingResponseOutputTextDeltaUpdate text:
                            partialText.Append(text.Delta);
                            if (onTextDelta is not null)
                                await onTextDelta(text.Delta);
                            cancellationToken.ThrowIfCancellationRequested();
                            break;
                        case StreamingResponseCompletedUpdate terminal:
                            completed = terminal.Response;
                            break;
                        case StreamingResponseFailedUpdate failed:
                            throw new InvalidOperationException(failed.Response.Error?.Message ?? "Responses 请求失败。");
                        case StreamingResponseIncompleteUpdate incomplete:
                            throw new InvalidOperationException($"Responses 请求未完成：{incomplete.Response.IncompleteStatusDetails?.Reason}。");
                        case StreamingResponseErrorUpdate error:
                            throw new InvalidOperationException(error.Message);
                    }
                }

                if (completed is null)
                    throw new InvalidOperationException("Responses 流未返回完成事件。");

                conversation.AppendResponse(completed);
                var terminalText = partialText.Length == 0 ? completed.GetOutputText() : null;
                partialText.Clear();
                if (onTextDelta is not null && terminalText is { Length: > 0 })
                    await onTextDelta(terminalText);
                cancellationToken.ThrowIfCancellationRequested();
                var calls = completed.OutputItems.OfType<FunctionCallResponseItem>().ToArray();
                if (calls.Length == 0)
                    return assistant.Content;

                foreach (var call in calls)
                {
                    var tool = tools.FirstOrDefault(candidate => candidate.Name == call.FunctionName);
                    var output = tool is null
                        ? ResponsesToolHelper.CreateOutput(call.CallId, $"未知的幻灯片工具：{call.FunctionName}。")
                        : await ResponsesToolHelper.InvokeAsync(tool, call, cancellationToken);
                    conversation.AppendItem(output);
                    assistant.ResponseInfo.AppendToolResult(output);
                }
            }
            throw new InvalidOperationException("Responses 超过幻灯片工具调用轮数限制。");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (partialText.Length > 0)
                conversation.AppendItem(ResponseItem.CreateAssistantMessageItem(partialText.ToString()));
            throw;
        }
        finally
        {
            conversation.RepairToolCallHistory();
            if (assistant.Content == CopilotChatMessage.PlaceholderContent)
                assistant.ClearMessageItems();
        }
    }

    internal void RestoreBefore(CopilotChatSession session, CopilotChatMessage targetMessage)
    {
        if (!_turnStarts.TryGetValue(targetMessage, out var history))
            throw new InvalidOperationException("找不到目标消息之前的 Responses 历史。");

        session.ResponsesSession.ApplyCompaction(history);
    }

    internal async Task CompactAsync(CancellationToken cancellationToken)
    {
        var conversation = _copilotChatManager.SelectedSession.ResponsesSession;
        if (conversation.Items.Count == 0)
            return;

        var client = await GetClientAsync(_copilotChatManager.AgentApiEndpointManager.PrimaryModel);
        using var content = BinaryContent.Create(BinaryData.FromObjectAsJson(new
        {
            model = client.ModelId,
            input = conversation.Items.Select(item => JsonSerializer.Deserialize<JsonElement>(ModelReaderWriter.Write(item).ToString())).ToArray(),
        }));
        var result = await client.Client.CompactResponseAsync(content, "application/json",
            new RequestOptions { CancellationToken = cancellationToken });
        using var document = JsonDocument.Parse(result.GetRawResponse().Content);
        var output = document.RootElement.GetProperty("output").EnumerateArray()
            .Select(item => ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(item.GetRawText()))
                ?? throw new InvalidDataException("Responses 压缩返回了空条目。"))
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        conversation.ApplyCompaction(output);
    }

    private static Task<ResponsesClientContext> GetClientAsync(ILanguageModel model) =>
        model is IResponsesClientProvider provider
            ? provider.GetResponsesClientAsync()
            : throw new NotSupportedException($"模型 {model.ModelDefinition.ModelName} 未配置 Responses API 客户端。");

    private static ResponseContentPart CreateInputPart(AIContent content) => content switch
    {
        TextContent text => ResponseContentPart.CreateInputTextPart(text.Text),
        DataContent image when image.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) =>
            ResponseContentPart.CreateInputImagePart(new BinaryData(image.Data, image.MediaType)),
        _ => throw new NotSupportedException($"Responses 不支持幻灯片输入类型 {content.GetType().Name}。"),
    };
}
