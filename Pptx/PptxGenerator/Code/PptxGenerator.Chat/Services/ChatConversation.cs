using System.IO;
using AgentLib.Tools;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace PptxGenerator.Chat.Services;

internal sealed record ChatImage(string Name, string MediaType, byte[] Data);
internal sealed record ChatTurn(ChatModel Model, string WorkPath, Uri? McpAddress, string Text, IReadOnlyList<ChatImage> Images);

internal sealed class ChatConversation
{
    internal static readonly string DefaultWorkPath = Path.Combine(AppContext.BaseDirectory, "Work");
    private readonly List<ResponseItem> _history = new();
    private WorkspaceToolProvider? _workspace;

    internal async Task RunAsync(ChatTurn turn, Action<int, string> appendText, Action<int, ToolActivity> updateTool,
        Action<string> addNotice, Action<string> workPathNotice, CancellationToken token)
    {
        var user = ResponseItem.CreateUserMessageItem(turn.Text.Length > 0 ? turn.Text : UiText.Get("PriorImage"));
        var inputs = new List<ResponseItem>(_history);
        var text = new SortedDictionary<int, System.Text.StringBuilder>();
        int offset = 0;
        var mcp = new McpTools();
        try
        {
            if (turn.Images.Count == 0)
            {
                inputs.Add(user);
            }
            else
            {
                var parts = new List<ResponseContentPart>();
                if (turn.Text.Length != 0) parts.Add(ResponseContentPart.CreateInputTextPart(turn.Text));
                foreach (var image in turn.Images)
                    parts.Add(ResponseContentPart.CreateInputImagePart(new BinaryData(image.Data, image.MediaType)));
                inputs.Add(ResponseItem.CreateUserMessageItem(parts));
            }

            var tools = new Dictionary<string, ChatTool>(StringComparer.Ordinal);
            if (turn.Model.SupportsTools)
            {
                AddFiles(turn.WorkPath, tools, message =>
                {
                    workPathNotice(message);
                    addNotice(message);
                });
                if (turn.McpAddress is not null)
                {
                    try
                    {
                        foreach (var tool in await mcp.DiscoverAsync(turn.McpAddress, token).ConfigureAwait(false))
                            tools.Add(tool.Key, tool.Value);
                    }
                    catch (Exception exception) when (!token.IsCancellationRequested && exception is not OutOfMemoryException)
                    {
                        ChatDiagnostics.Failure("DiscoverMcp", exception, "Host", turn.McpAddress.Host);
                        addNotice(UiText.Get("McpUnavailable"));
                    }
                }
            }

            while (true)
            {
                token.ThrowIfCancellationRequested();
                var request = new CreateResponseOptions(turn.Model.Id, inputs)
                {
                    StreamingEnabled = true,
                    StoredOutputEnabled = false,
                    Instructions = UiText.Get("Instructions"),
                };
                if (turn.Model.Definition.Capabilities?.Reasoning == true)
                    request.IncludedProperties.Add(IncludedResponseProperty.ReasoningEncryptedContent);
                foreach (var tool in tools.Values) request.Tools.Add(tool.Definition);

                ResponseResult? response = null;
                var doneItems = new SortedDictionary<int, ResponseItem>();
                var displayedCalls = new Dictionary<string, (int Index, ToolActivity Activity)>(StringComparer.Ordinal);
                await foreach (var update in turn.Model.Client.CreateResponseStreamingAsync(request, token).ConfigureAwait(false))
                {
                    switch (update)
                    {
                        case StreamingResponseOutputTextDeltaUpdate delta:
                            Append(offset + delta.OutputIndex, delta.Delta);
                            break;
                        case StreamingResponseRefusalDeltaUpdate refusal:
                            Append(offset + refusal.OutputIndex, refusal.Delta);
                            break;
                        case StreamingResponseOutputItemDoneUpdate done:
                            doneItems[done.OutputIndex] = done.Item;
                            if (done.Item is FunctionCallResponseItem finishedCall)
                                ShowCall(offset + done.OutputIndex, finishedCall);
                            break;
                        case StreamingResponseCompletedUpdate completed:
                            response = completed.Response;
                            break;
                        case StreamingResponseFailedUpdate failed:
                            throw new InvalidOperationException($"Responses stream failed. Code={failed.Response.Error?.Code}.");
                        case StreamingResponseIncompleteUpdate:
                            addNotice(UiText.Get("ReplyIncomplete"));
                            return;
                        case StreamingResponseErrorUpdate:
                            throw new InvalidOperationException("Responses stream reported a protocol error.");
                    }
                }

                token.ThrowIfCancellationRequested();
                if (response?.Status != ResponseStatus.Completed)
                    throw new InvalidOperationException("Responses stream ended without a completed response.");
                if (response.OutputItems.Count != 0)
                {
                    foreach (var (item, index) in response.OutputItems.Select((item, index) => (item, index)))
                        doneItems.TryAdd(index, item);
                }
                foreach (var (index, item) in doneItems)
                {
                    if (item is MessageResponseItem message && !text.ContainsKey(offset + index))
                        foreach (var part in message.Content) Append(offset + index, part.Text ?? part.Refusal ?? string.Empty);
                    if (item is FunctionCallResponseItem call) ShowCall(offset + index, call);
                }

                var calls = doneItems.Values.OfType<FunctionCallResponseItem>().ToArray();
                if (calls.Length == 0) return;
                inputs.AddRange(doneItems.Values);
                foreach (var call in calls)
                {
                    token.ThrowIfCancellationRequested();
                    var (index, activity) = displayedCalls[call.CallId];
                    if (!tools.TryGetValue(call.FunctionName, out var tool))
                    {
                        var unavailable = UiText.Get("ToolUnavailable");
                        updateTool(index, activity with { State = UiText.Get("ToolError"), Result = unavailable });
                        inputs.Add(ResponseItem.CreateFunctionCallOutputItem(call.CallId, unavailable));
                        continue;
                    }

                    updateTool(index, activity with { State = UiText.Get("ToolRunning") });
                    try
                    {
                        var result = await tool.InvokeAsync(call, token).ConfigureAwait(false);
                        inputs.Add(result.Output);
                        updateTool(index, activity with
                        {
                            State = UiText.Get(result.IsError ? "ToolError" : "ToolReturned"),
                            Result = ToolDetailFormatter.Format(result.DisplayText ?? result.Output.FunctionOutput),
                            Images = result.Images,
                        });
                    }
                    catch (Exception exception) when (exception is not OutOfMemoryException)
                    {
                        ChatDiagnostics.Failure("InvokeTool", exception, "ToolName", call.FunctionName);
                        if (tool.Remote)
                        {
                            updateTool(index, activity with { State = UiText.Get("ToolUnknown"), Result = UiText.Get("McpOutcomeUnknown") });
                            token.ThrowIfCancellationRequested();
                            return;
                        }
                        updateTool(index, activity with
                        {
                            State = UiText.Get(token.IsCancellationRequested ? "ToolStopped" : "ToolError"),
                            Result = UiText.Get("ToolUnavailable"),
                        });
                        token.ThrowIfCancellationRequested();
                        inputs.Add(ResponseItem.CreateFunctionCallOutputItem(call.CallId,
                            $"{UiText.Get("ToolUnavailable")} ({exception.GetType().Name})"));
                    }
                }

                offset = Math.Max(offset + doneItems.Keys.Last(), text.Count > 0 ? text.Keys.Last() : offset) + 1;

                void ShowCall(int index, FunctionCallResponseItem call)
                {
                    if (displayedCalls.ContainsKey(call.CallId)) return;
                    var label = tools.TryGetValue(call.FunctionName, out var tool) ? tool.Label : call.FunctionName;
                    var activity = new ToolActivity(label, call.FunctionName, ToolDetailFormatter.Format(call.FunctionArguments?.ToString()),
                        UiText.Get("ToolPending"), string.Empty);
                    displayedCalls.Add(call.CallId, (index, activity));
                    updateTool(index, activity);
                }

                void Append(int index, string delta)
                {
                    if (delta.Length == 0) return;
                    if (!text.TryGetValue(index, out var block)) text.Add(index, block = new System.Text.StringBuilder());
                    block.Append(delta);
                    appendText(index, delta);
                }
            }
        }
        finally
        {
            // Attachments and tool outputs stay in the active request; only visible text carries across models.
            _history.Add(user);
            if (text.Count > 0) _history.Add(ResponseItem.CreateAssistantMessageItem(string.Join("\n\n", text.Values)));
            await mcp.CloseAsync().ConfigureAwait(false);
        }
    }

    private void AddFiles(string path, Dictionary<string, ChatTool> tools, Action<string> notice)
    {
        try
        {
            string? fullPath = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path, AppContext.BaseDirectory);
            if (string.Equals(fullPath, DefaultWorkPath, StringComparison.OrdinalIgnoreCase))
                Directory.CreateDirectory(DefaultWorkPath);
            if (!Directory.Exists(fullPath))
            {
                _workspace = null;
                notice(UiText.Get("WorkPathInvalid"));
                return;
            }
            if (!string.Equals(_workspace?.WorkspacePath, fullPath, StringComparison.OrdinalIgnoreCase))
                _workspace = new WorkspaceToolProvider { WorkspacePath = fullPath };

            foreach (var function in _workspace!.CreateDefaultTools().OfType<AIFunction>())
            {
                string? label = function.Name switch
                {
                    nameof(WorkspaceToolProvider.ReadFileLines) => "ToolRead",
                    nameof(WorkspaceToolProvider.WriteFileContent) => "ToolWrite",
                    _ => null,
                };
                if (label is not null)
                    tools.Add(function.Name, new ChatTool(ResponsesToolHelper.CreateTool(function), UiText.Get(label),
                        false, async (call, token) => new ToolResult(
                            await ResponsesToolHelper.InvokeAsync(function, call, token).ConfigureAwait(false))));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _workspace = null;
            ChatDiagnostics.Failure("OpenWorkspace", exception, "WorkPath", path);
            notice(UiText.Get("WorkPathInvalid"));
        }
    }
}
