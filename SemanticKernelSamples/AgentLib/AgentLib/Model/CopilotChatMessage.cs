using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace AgentLib.Model;

public sealed class CopilotChatMessage : NotifyBase, ICopilotChatCurrentContent
{
    public CopilotChatMessage(ChatRole role, string content)
        : this(role, [new TextContent(content ?? "")])
    {
    }

    /// <summary>
    /// 从 <see cref="AIContent"/> 集合构建消息，支持文本、图片、音频等多模态内容。
    /// </summary>
    public CopilotChatMessage(ChatRole role, IReadOnlyList<AIContent> contents)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Role = role;
        MessageItems.CollectionChanged += MessageItems_CollectionChanged;

        foreach (AIContent content in contents)
        {
            switch (content)
            {
                case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                    MessageItems.Add(new CopilotChatTextItem(textContent.Text));
                    break;
                case DataContent dataContent when dataContent.Data is { Length: > 0 }:
                    MessageItems.Add(CreateDataItem(dataContent));
                    break;
            }
        }

        CreatedTime = DateTimeOffset.Now;
        TimeText = CreatedTime.ToString("HH:mm");
    }

    private static ICopilotChatMessageItem CreateDataItem(DataContent dataContent)
    {
        ReadOnlyMemory<byte> data = dataContent.Data;
        string mediaType = dataContent.MediaType ?? string.Empty;
        if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return new CopilotChatImageItem(BinaryData.FromBytes(data), mediaType);
        }

        if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return new CopilotChatAudioItem(BinaryData.FromBytes(data), mediaType);
        }

        // 其他二进制数据统一当作图片处理，保留原始 mediaType
        return new CopilotChatImageItem(BinaryData.FromBytes(data), string.IsNullOrWhiteSpace(mediaType) ? "application/octet-stream" : mediaType);
    }

    public ChatRole Role { get; }

    /// <summary>
    /// 是否预设的内容，预设内容不参与 GPT 信息
    /// </summary>
    public bool IsPresetInfo { set; get; }

    public string Author
    {
        get
        {
            if (Role == ChatRole.System)
            {
                return "系统";
            }
            else if (Role == ChatRole.User)
            {
                return "我";
            }
            else if (Role == ChatRole.Assistant)
            {
                return "Copilot";
            }

            return string.Empty;
        }
    }

    public ObservableCollection<ICopilotChatMessageItem> MessageItems { get; } = [];

    public string Content => string.Concat(MessageItems.OfType<CopilotChatTextItem>().Select(item => item.Text));

    public string Reason => string.Concat(MessageItems.OfType<CopilotChatReasoningItem>().Select(item => item.Text));

    public bool HasContent => !string.IsNullOrEmpty(Content);

    public bool HasReason => !string.IsNullOrEmpty(Reason);

    public bool HasReasonAndContent => HasReason && HasContent;

    public UsageDetails? UsageDetails
    {
        get => _usageDetails;
        private set
        {
            if (ReferenceEquals(value, _usageDetails))
            {
                return;
            }

            _usageDetails = value;
            OnUsageDetailsChanged();
        }
    }

    private UsageDetails? _usageDetails;
    private const string UsageSummarySeparator = " ";

    public bool HasUsageDetails => UsageDetails is not null;

    public bool HasTotalTokenCount => UsageDetails?.TotalTokenCount is not null;

    public string TotalTokenCountText => UsageDetails?.TotalTokenCount is { } totalTokenCount
        ? $"总计 {totalTokenCount:N0}"
        : string.Empty;

    public bool HasInputTokenCount => UsageDetails?.InputTokenCount is not null;

    public string InputTokenCountText
    {
        get
        {
            if (UsageDetails?.InputTokenCount is { } inputTokenCount)
            {
                return $"输入 {inputTokenCount:N0}";
            }

            return string.Empty;
        }
    }

    public bool HasOutputTokenCount => UsageDetails?.OutputTokenCount is not null;

    public string OutputTokenCountText
    {
        get
        {
            if (UsageDetails?.OutputTokenCount is { } outputTokenCount)
            {
                return $"输出 {outputTokenCount:N0}";
            }

            return string.Empty;
        }
    }

    public bool HasReasoningTokenCount => UsageDetails?.ReasoningTokenCount is not null;

    public string ReasoningTokenCountText => UsageDetails?.ReasoningTokenCount is { } reasoningTokenCount
        ? $"思考 {reasoningTokenCount:N0}"
        : string.Empty;

    public bool HasCachedInputTokenCount => UsageDetails?.CachedInputTokenCount is not null;

    public string CachedInputTokenCountText => UsageDetails?.CachedInputTokenCount is { } cachedInputTokenCount
        ? $"缓存 {cachedInputTokenCount:N0}"
        : string.Empty;

    public string UsageSummaryText
    {
        get
        {
            if (UsageDetails is null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            if (HasTotalTokenCount)
            {
                parts.Add(TotalTokenCountText);
            }

            if (HasInputTokenCount)
            {
                parts.Add(InputTokenCountText);
            }

            if (HasOutputTokenCount)
            {
                parts.Add(OutputTokenCountText);
            }

            if (HasReasoningTokenCount)
            {
                parts.Add(ReasoningTokenCountText);
            }

            if (HasCachedInputTokenCount)
            {
                parts.Add(CachedInputTokenCountText);
            }

            if (parts.Count == 0)
            {
                return string.Empty;
            }

            return $"用量{string.Join(UsageSummarySeparator, parts)}";
        }
    }

    public string FullContent
    {
        get
        {
            if (MessageItems.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();

            foreach (ICopilotChatMessageItem messageItem in MessageItems)
            {
                string? itemText = FormatMessageItem(messageItem);
                if (string.IsNullOrEmpty(itemText))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("--------");
                }

                builder.Append(itemText);
            }

            return builder.ToString();
        }
    }

    public DateTimeOffset CreatedTime { get; }

    public string TimeText { get; }

    public static CopilotChatMessage CreateUser(string content)
    {
        return new CopilotChatMessage(ChatRole.User, content);
    }

    /// <summary>
    /// 从多模态内容集合创建用户消息。
    /// </summary>
    public static CopilotChatMessage CreateUser(IReadOnlyList<AIContent> contents)
    {
        return new CopilotChatMessage(ChatRole.User, contents);
    }

    public static CopilotChatMessage CreateAssistant(string content, bool isPresetInfo)
    {
        return new CopilotChatMessage(ChatRole.Assistant, content)
        {
            IsPresetInfo = isPresetInfo
        };
    }

    public void AppendUsageDetails(IEnumerable<AIContent>? contents)
    {
        if (Role != ChatRole.Assistant || contents is null)
        {
            return;
        }

        foreach (AIContent content in contents)
        {
            if (content is UsageContent usageContent)
            {
                AppendUsageDetails(usageContent);
            }
        }
    }

    public ChatMessage ToChatMessage()
    {
        var contents = new List<AIContent>(MessageItems.Count);
        foreach (ICopilotChatMessageItem messageItem in MessageItems)
        {
            switch (messageItem)
            {
                case CopilotChatTextItem textItem when !string.IsNullOrEmpty(textItem.Text):
                    contents.Add(new TextContent(textItem.Text));
                    break;
                case CopilotChatImageItem imageItem:
                    contents.Add(new DataContent(imageItem.Data.ToMemory(), imageItem.MimeType));
                    break;
                case CopilotChatAudioItem audioItem:
                    contents.Add(new DataContent(audioItem.Data.ToMemory(), audioItem.MimeType));
                    break;
            }
        }

        return new ChatMessage(Role, contents);
    }

    public void ClearMessageItems()
    {
        foreach (INotifyPropertyChanged messageItem in MessageItems.OfType<INotifyPropertyChanged>())
        {
            messageItem.PropertyChanged -= MessageItem_PropertyChanged;
        }

        MessageItems.Clear();
        _toolItemsByCallId.Clear();
        _subAgentItemsByCallId.Clear();
    }

    public void AppendText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (MessageItems.LastOrDefault() is CopilotChatTextItem lastTextItem)
        {
            lastTextItem.Text += text;
            return;
        }

        MessageItems.Add(new CopilotChatTextItem(text));
    }

    public void AppendReasoning(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (MessageItems.LastOrDefault() is CopilotChatReasoningItem lastReasoningItem)
        {
            lastReasoningItem.Text += text;
            return;
        }

        MessageItems.Add(new CopilotChatReasoningItem(text));
    }

    public void RegisterApprovalTool(string toolName, string? approvalDescription = null)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        _approvalToolDescriptions[toolName] = approvalDescription;
    }

    public void AppendFunctionCall(FunctionCallContent functionCallContent)
    {
        ArgumentNullException.ThrowIfNull(functionCallContent);

        if (string.Equals(functionCallContent.Name, "InvokeSubAgent", StringComparison.Ordinal))
        {
            AppendSubAgentCall(functionCallContent);
            return;
        }

        if (IsApprovalTool(functionCallContent.Name))
        {
            AppendApprovalToolCall(functionCallContent);
            return;
        }

        string callId = string.IsNullOrWhiteSpace(functionCallContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionCallContent.CallId;

        if (!_toolItemsByCallId.TryGetValue(callId, out CopilotChatToolItem? toolItem))
        {
            toolItem = new CopilotChatToolItem(callId, functionCallContent.Name, CopilotChatMessageItemFormatter.FormatArguments(functionCallContent));
            _toolItemsByCallId[callId] = toolItem;
            MessageItems.Add(toolItem);
            return;
        }

        toolItem.ToolName = functionCallContent.Name;
        toolItem.InputText = CopilotChatMessageItemFormatter.FormatArguments(functionCallContent) ?? string.Empty;
    }

    public void AppendFunctionResult(FunctionResultContent functionResultContent)
    {
        ArgumentNullException.ThrowIfNull(functionResultContent);

        if (_subAgentItemsByCallId.ContainsKey(functionResultContent.CallId ?? string.Empty))
        {
            AppendSubAgentResult(functionResultContent);
            return;
        }

        if (_approvalToolItemsByCallId.ContainsKey(functionResultContent.CallId ?? string.Empty))
        {
            AppendApprovalToolResult(functionResultContent);
            return;
        }

        string callId = string.IsNullOrWhiteSpace(functionResultContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionResultContent.CallId;

        if (!_toolItemsByCallId.TryGetValue(callId, out CopilotChatToolItem? toolItem))
        {
            toolItem = new CopilotChatToolItem(callId, "工具", null);
            _toolItemsByCallId[callId] = toolItem;
            MessageItems.Add(toolItem);
        }

        toolItem.OutputText = CopilotChatMessageItemFormatter.FormatArgumentsToHumans(functionResultContent) ?? string.Empty;
    }

    public CopilotChatApprovalToolItem CreateApprovalToolItem(string toolName, string? inputText, string? approvalDescription = null,
        string? callId = null)
    {
        string resolvedCallId = string.IsNullOrWhiteSpace(callId)
            ? Guid.NewGuid().ToString("N")
            : callId;
        string normalizedToolName = string.IsNullOrWhiteSpace(toolName) ? "工具" : toolName;
        string normalizedInputText = inputText ?? string.Empty;
        string? resolvedApprovalDescription = ResolveApprovalDescription(normalizedToolName, approvalDescription);

        if (!string.IsNullOrWhiteSpace(callId) && _approvalToolItemsByCallId.TryGetValue(resolvedCallId, out CopilotChatApprovalToolItem? existingItem))
        {
            existingItem.ToolName = normalizedToolName;
            existingItem.InputText = normalizedInputText;
            existingItem.ApprovalDescription = resolvedApprovalDescription;
            return existingItem;
        }

        CopilotChatApprovalToolItem? pendingItem = MessageItems
            .OfType<CopilotChatApprovalToolItem>()
            .LastOrDefault(item => item.IsPendingApproval
                                  && string.Equals(item.ToolName, normalizedToolName, StringComparison.Ordinal)
                                  && string.Equals(item.InputText, normalizedInputText, StringComparison.Ordinal));

        if (pendingItem is not null)
        {
            if (!_approvalToolItemsByCallId.ContainsKey(resolvedCallId))
            {
                pendingItem.CallId = resolvedCallId;
                _approvalToolItemsByCallId[resolvedCallId] = pendingItem;
            }

            pendingItem.ApprovalDescription = resolvedApprovalDescription;
            return pendingItem;
        }

        var approvalToolItem = new CopilotChatApprovalToolItem(resolvedCallId, normalizedToolName, normalizedInputText, resolvedApprovalDescription);
        _approvalToolItemsByCallId[resolvedCallId] = approvalToolItem;
        MessageItems.Add(approvalToolItem);
        return approvalToolItem;
    }

    private void AppendUsageDetails(UsageContent usageContent)
    {
        if (Role != ChatRole.Assistant || usageContent.Details is null)
        {
            return;
        }

        AppendUsageDetails(usageContent.Details);
    }

    public void AppendUsageDetails(UsageDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (UsageDetails is null)
        {
            var usageDetails = new UsageDetails();
            usageDetails.Add(details);
            UsageDetails = usageDetails;
            return;
        }

        UsageDetails.Add(details);
        OnUsageDetailsChanged();
    }

    public CopilotChatSubAgentItem CreateSubAgentItem(string toolName, string? inputText, string? callId = null)
    {
        string resolvedCallId = string.IsNullOrWhiteSpace(callId)
            ? Guid.NewGuid().ToString("N")
            : callId;

        if (!_subAgentItemsByCallId.TryGetValue(resolvedCallId, out CopilotChatSubAgentItem? subAgentItem))
        {
            subAgentItem = new CopilotChatSubAgentItem(resolvedCallId, toolName, inputText);
            _subAgentItemsByCallId[resolvedCallId] = subAgentItem;
            MessageItems.Add(subAgentItem);
            return subAgentItem;
        }

        subAgentItem.ToolName = toolName;
        subAgentItem.InputText = inputText ?? string.Empty;
        return subAgentItem;
    }

    private void OnUsageDetailsChanged()
    {
        OnPropertyChanged(nameof(UsageDetails));
        OnPropertyChanged(nameof(HasUsageDetails));
        OnPropertyChanged(nameof(HasTotalTokenCount));
        OnPropertyChanged(nameof(TotalTokenCountText));
        OnPropertyChanged(nameof(HasInputTokenCount));
        OnPropertyChanged(nameof(InputTokenCountText));
        OnPropertyChanged(nameof(HasOutputTokenCount));
        OnPropertyChanged(nameof(OutputTokenCountText));
        OnPropertyChanged(nameof(HasReasoningTokenCount));
        OnPropertyChanged(nameof(ReasoningTokenCountText));
        OnPropertyChanged(nameof(HasCachedInputTokenCount));
        OnPropertyChanged(nameof(CachedInputTokenCountText));
        OnPropertyChanged(nameof(UsageSummaryText));
    }

    private readonly Dictionary<string, CopilotChatToolItem> _toolItemsByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CopilotChatApprovalToolItem> _approvalToolItemsByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CopilotChatSubAgentItem> _subAgentItemsByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _approvalToolDescriptions = new(StringComparer.Ordinal);

    private static string? FormatMessageItem(ICopilotChatMessageItem messageItem)
    {
        return messageItem switch
        {
            CopilotChatTextItem textItem => textItem.Text,
            CopilotChatReasoningItem reasoningItem => $"思考：{Environment.NewLine}{reasoningItem.Text}",
            CopilotChatImageItem imageItem => imageItem.DisplayText,
            CopilotChatAudioItem audioItem => audioItem.DisplayText,
            CopilotChatApprovalToolItem approvalToolItem => FormatApprovalToolItem(approvalToolItem),
            CopilotChatToolItem toolItem => FormatToolItem(toolItem),
            CopilotChatSubAgentItem subAgentItem => FormatSubAgentItem(subAgentItem),
            _ => null
        };
    }

    private static string FormatApprovalToolItem(CopilotChatApprovalToolItem approvalToolItem)
    {
        var builder = new StringBuilder();
        builder.Append("审批工具：").Append(approvalToolItem.ToolName);
        builder.AppendLine()
            .Append("审批状态：")
            .Append(approvalToolItem.ApprovalStateText);

        if (approvalToolItem.HasApprovalDescription)
        {
            builder.AppendLine()
                .Append("审批说明：")
                .AppendLine()
                .Append(approvalToolItem.ApprovalDescription);
        }

        if (approvalToolItem.HasInputText)
        {
            builder.AppendLine()
                .Append("输入：")
                .AppendLine()
                .Append(approvalToolItem.InputText);
        }

        if (approvalToolItem.HasDecisionReason)
        {
            builder.AppendLine()
                .Append("审批备注：")
                .AppendLine()
                .Append(approvalToolItem.DecisionReason);
        }

        if (approvalToolItem.HasOutputText)
        {
            builder.AppendLine()
                .Append("输出：")
                .AppendLine()
                .Append(approvalToolItem.OutputText);
        }

        return builder.ToString();
    }

    private static string FormatToolItem(CopilotChatToolItem toolItem)
    {
        var builder = new StringBuilder();
        builder.Append("工具：").Append(toolItem.ToolName);

        if (toolItem.HasInputText)
        {
            builder.AppendLine()
                .Append("输入：")
                .AppendLine()
                .Append(toolItem.InputText);
        }

        if (toolItem.HasOutputText)
        {
            builder.AppendLine()
                .Append("输出：")
                .AppendLine()
                .Append(toolItem.OutputText);
        }

        return builder.ToString();
    }

    private static string FormatSubAgentItem(CopilotChatSubAgentItem subAgentItem)
    {
        var builder = new StringBuilder();
        builder.Append("子代理：").Append(subAgentItem.ToolName);

        if (subAgentItem.HasInputText)
        {
            builder.AppendLine()
                .Append("输入：")
                .AppendLine()
                .Append(subAgentItem.InputText);
        }

        foreach (ICopilotChatMessageItem messageItem in subAgentItem.MessageItems)
        {
            string? itemText = FormatMessageItem(messageItem);
            if (string.IsNullOrEmpty(itemText))
            {
                continue;
            }

            builder.AppendLine()
                .Append("进度：")
                .AppendLine()
                .Append(itemText);
        }

        if (subAgentItem.HasOutputText)
        {
            builder.AppendLine()
                .Append("输出：")
                .AppendLine()
                .Append(subAgentItem.OutputText);
        }

        return builder.ToString();
    }

    private void AppendSubAgentCall(FunctionCallContent functionCallContent)
    {
        string callId = string.IsNullOrWhiteSpace(functionCallContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionCallContent.CallId;

        CopilotChatSubAgentItem subAgentItem = CreateSubAgentItem(functionCallContent.Name, CopilotChatMessageItemFormatter.FormatArguments(functionCallContent), callId);
        subAgentItem.ToolName = functionCallContent.Name;
    }

    private void AppendSubAgentResult(FunctionResultContent functionResultContent)
    {
        string callId = string.IsNullOrWhiteSpace(functionResultContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionResultContent.CallId;

        if (_subAgentItemsByCallId.TryGetValue(callId, out CopilotChatSubAgentItem? subAgentItem))
        {
            subAgentItem.OutputText = CopilotChatMessageItemFormatter.FormatResult(functionResultContent) ?? string.Empty;
        }
    }

    private void AppendApprovalToolCall(FunctionCallContent functionCallContent)
    {
        string callId = string.IsNullOrWhiteSpace(functionCallContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionCallContent.CallId;

        _ = CreateApprovalToolItem(functionCallContent.Name, CopilotChatMessageItemFormatter.FormatArguments(functionCallContent),
            ResolveApprovalDescription(functionCallContent.Name), callId);
    }

    private void AppendApprovalToolResult(FunctionResultContent functionResultContent)
    {
        string callId = string.IsNullOrWhiteSpace(functionResultContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionResultContent.CallId;

        if (_approvalToolItemsByCallId.TryGetValue(callId, out CopilotChatApprovalToolItem? approvalToolItem))
        {
            approvalToolItem.OutputText = CopilotChatMessageItemFormatter.FormatArgumentsToHumans(functionResultContent) ?? string.Empty;
        }
    }

    private bool IsApprovalTool(string? toolName)
    {
        return !string.IsNullOrWhiteSpace(toolName) && _approvalToolDescriptions.ContainsKey(toolName);
    }

    private string? ResolveApprovalDescription(string? toolName, string? fallbackApprovalDescription = null)
    {
        if (!string.IsNullOrWhiteSpace(toolName) && _approvalToolDescriptions.TryGetValue(toolName, out string? approvalDescription))
        {
            return approvalDescription;
        }

        return fallbackApprovalDescription;
    }

    private void MessageItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (INotifyPropertyChanged messageItem in e.OldItems.OfType<INotifyPropertyChanged>())
            {
                messageItem.PropertyChanged -= MessageItem_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (INotifyPropertyChanged messageItem in e.NewItems.OfType<INotifyPropertyChanged>())
            {
                messageItem.PropertyChanged += MessageItem_PropertyChanged;
            }
        }

        OnMessageItemsChanged();
    }

    private void MessageItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnMessageItemsChanged();
    }

    private void OnMessageItemsChanged()
    {
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(Reason));
        OnPropertyChanged(nameof(HasContent));
        OnPropertyChanged(nameof(HasReason));
        OnPropertyChanged(nameof(HasReasonAndContent));
        OnPropertyChanged(nameof(FullContent));
    }
}
