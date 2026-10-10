#pragma warning disable OPENAI001 // 原生 Responses API 当前标记为实验性。

using System.ClientModel.Primitives;
using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace AgentLib.Model;

/// <summary>
/// 一条消息的 Responses 信息及增量更新逻辑。仅在显式进入 Responses 路线时创建。
/// </summary>
public sealed class CopilotChatMessageResponseInfo
{
    private readonly CopilotChatMessage _message;

    internal CopilotChatMessageResponseInfo(CopilotChatMessage message)
    {
        _message = message;
    }

    /// <summary>
    /// 当前原生响应，尚未收到时为空。
    /// </summary>
    public ResponseResult? CurrentResponse => _responses.LastOrDefault();

    private readonly HashSet<string> _reportedUsage = new(StringComparer.Ordinal);
    private readonly List<ResponseResult> _responses = [];
    private readonly Dictionary<(int Output, int Part, bool Reasoning), ICopilotChatMessageItem> _responseParts = [];
    private readonly Dictionary<int, CopilotChatToolItem> _responseTools = [];
    private string? _activeResponseId;
    private bool _hasResponseContent;

    /// <summary>
    /// 已接收的原生响应记录。集合只读，SDK 对象不做深拷贝；调用方不应修改已提交的响应。
    /// </summary>
    public IReadOnlyList<ResponseResult> Responses => _readOnlyResponses ??= _responses.AsReadOnly();
    private ReadOnlyCollection<ResponseResult>? _readOnlyResponses;

    /// <summary>
    /// 接收原生 Responses 流式事件。调用方负责在消息所属线程调用，并负责请求、工具执行和取消。
    /// </summary>
    public void AppendResponseUpdate(StreamingResponseUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        switch (update)
        {
            case StreamingResponseCreatedUpdate created:
                BeginResponse(created.Response.Id);
                StoreResponse(created.Response);
                break;
            case StreamingResponseInProgressUpdate progress:
                StoreResponse(progress.Response);
                break;
            case StreamingResponseQueuedUpdate queued:
                StoreResponse(queued.Response);
                break;
            case StreamingResponseOutputTextDeltaUpdate text:
                SetResponseText(text.OutputIndex, text.ContentIndex, text.Delta, false, true);
                break;
            case StreamingResponseOutputTextDoneUpdate text:
                SetResponseText(text.OutputIndex, text.ContentIndex, text.Text, false, false);
                break;
            case StreamingResponseRefusalDeltaUpdate refusal:
                SetResponseText(refusal.OutputIndex, refusal.ContentIndex, refusal.Delta, false, true);
                break;
            case StreamingResponseRefusalDoneUpdate refusal:
                SetResponseText(refusal.OutputIndex, refusal.ContentIndex, refusal.Refusal, false, false);
                break;
            case StreamingResponseReasoningSummaryTextDeltaUpdate reasoning:
                SetResponseText(reasoning.OutputIndex, reasoning.SummaryIndex, reasoning.Delta, true, true);
                break;
            case StreamingResponseReasoningSummaryTextDoneUpdate reasoning:
                SetResponseText(reasoning.OutputIndex, reasoning.SummaryIndex, reasoning.Text, true, false);
                break;
            case StreamingResponseOutputItemAddedUpdate added:
                ApplyResponseItem(added.OutputIndex, added.Item);
                break;
            case StreamingResponseOutputItemDoneUpdate done:
                ApplyResponseItem(done.OutputIndex, done.Item);
                break;
            case StreamingResponseFunctionCallArgumentsDeltaUpdate arguments:
                _responseTools[arguments.OutputIndex].InputText += arguments.Delta.ToString();
                break;
            case StreamingResponseFunctionCallArgumentsDoneUpdate arguments:
                _responseTools[arguments.OutputIndex].InputText = arguments.FunctionArguments.ToString();
                break;
            case StreamingResponseWebSearchCallInProgressUpdate search:
                GetResponseTool(search.OutputIndex, search.ItemId, "web_search").OutputText = "in_progress";
                break;
            case StreamingResponseWebSearchCallSearchingUpdate search:
                GetResponseTool(search.OutputIndex, search.ItemId, "web_search").OutputText = "searching";
                break;
            case StreamingResponseWebSearchCallCompletedUpdate search:
                GetResponseTool(search.OutputIndex, search.ItemId, "web_search").OutputText = "completed";
                break;
            case StreamingResponseErrorUpdate error:
                var response = _responses.LastOrDefault() ?? new ResponseResult { Id = _activeResponseId };
                response.Error = ModelReaderWriter.Read<ResponseError>(BinaryData.FromObjectAsJson(new
                {
                    code = error.Code, message = error.Message, param = error.Param,
                }));
                response.Status = ResponseStatus.Failed;
                StoreResponse(response);
                break;
            case StreamingResponseCompletedUpdate completed:
                AppendResponse(completed.Response);
                break;
            case StreamingResponseIncompleteUpdate incomplete:
                AppendResponse(incomplete.Response);
                break;
            case StreamingResponseFailedUpdate failed:
                AppendResponse(failed.Response);
                break;
        }
    }

    /// <summary>
    /// 接收完整或流式终态响应；同一响应的终态校准展示内容，不重复追加文本和用量。
    /// </summary>
    public void AppendResponse(ResponseResult response)
    {
        ArgumentNullException.ThrowIfNull(response);
        BeginResponse(response.Id);
        for (int index = 0; index < response.OutputItems.Count; index++)
        {
            ApplyResponseItem(index, response.OutputItems[index]);
        }
        StoreResponse(response);
        UpdateResponseUsage();
    }

    /// <summary>
    /// 展示本地函数执行结果；同一个原生 Item 可直接用于下一轮 Responses 请求。
    /// </summary>
    public void AppendToolResult(FunctionCallOutputResponseItem output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var tool = _message.MessageItems.OfType<CopilotChatToolItem>().Last(item => item.CallId == output.CallId);
        tool.OutputText = output.FunctionOutput;
    }

    private void BeginResponse(string id)
    {
        if (_activeResponseId == id)
        {
            return;
        }
        _activeResponseId = id;
        _responseParts.Clear();
        _responseTools.Clear();
    }

    private void PrepareResponseContent()
    {
        if (_hasResponseContent)
        {
            return;
        }
        _hasResponseContent = true;
        if (_message.MessageItems.Count == 1 && _message.MessageItems[0] is CopilotChatTextItem { Text: CopilotChatMessage.PlaceholderContent })
        {
            _message.MessageItems.Clear();
        }
    }

    private void SetResponseText(int output, int part, string text, bool reasoning, bool delta)
    {
        PrepareResponseContent();
        var key = (output, part, reasoning);
        bool isNew = !_responseParts.TryGetValue(key, out var item);
        if (isNew)
        {
            item = reasoning ? new CopilotChatReasoningItem(string.Empty) : new CopilotChatTextItem(string.Empty);
            _responseParts.Add(key, item);
            _message.MessageItems.Add(item);
        }
        if (item is CopilotChatTextItem targetText)
        {
            if (delta || isNew) targetText.AppendText(text);
            else targetText.Text = text;
        }
        else if (item is CopilotChatReasoningItem targetReasoning)
        {
            if (delta || isNew) targetReasoning.AppendText(text);
            else targetReasoning.Text = text;
        }
    }

    private void ApplyResponseItem(int index, ResponseItem item)
    {
        switch (item)
        {
            case MessageResponseItem message:
                for (int part = 0; part < message.Content.Count; part++)
                {
                    var content = message.Content[part];
                    SetResponseText(index, part, content.Text ?? content.Refusal ?? string.Empty, false, false);
                }
                break;
            case ReasoningResponseItem reasoning:
                for (int part = 0; part < reasoning.SummaryParts.Count; part++)
                {
                    if (reasoning.SummaryParts[part] is ReasoningSummaryTextPart text)
                        SetResponseText(index, part, text.Text, true, false);
                }
                break;
            case FunctionCallResponseItem function:
                var tool = GetResponseTool(index, function.CallId, function.FunctionName);
                tool.InputText = function.FunctionArguments?.ToString() ?? string.Empty;
                break;
            case WebSearchCallResponseItem search:
                var searchTool = GetResponseTool(index, search.Id, "web_search");
                searchTool.InputText = search.Action is null ? string.Empty : ModelReaderWriter.Write(search.Action).ToString();
                searchTool.OutputText = search.Status?.ToString() ?? string.Empty;
                break;
        }
    }

    private CopilotChatToolItem GetResponseTool(int index, string callId, string name)
    {
        PrepareResponseContent();
        if (!_responseTools.TryGetValue(index, out var tool))
        {
            tool = new CopilotChatToolItem(callId, name, string.Empty);
            _responseTools.Add(index, tool);
            _message.MessageItems.Add(tool);
        }
        return tool;
    }

    private void StoreResponse(ResponseResult response)
    {
        int index = _responses.FindIndex(item => item.Id == response.Id);
        if (index < 0) _responses.Add(response);
        else _responses[index] = response;
    }

    private void UpdateResponseUsage()
    {

        foreach (var response in _responses)
        {
            if (response.Usage is not { } usage || !_reportedUsage.Add(response.Id)) continue;
            var current = new UsageDetails
            {
                InputTokenCount = usage.InputTokenCount,
                OutputTokenCount = usage.OutputTokenCount,
                TotalTokenCount = usage.TotalTokenCount,
                CachedInputTokenCount = usage.InputTokenDetails?.CachedTokenCount,
                ReasoningTokenCount = usage.OutputTokenDetails?.ReasoningTokenCount,
            };
            _message.AppendUsageDetails(current);
        }
    }

}
