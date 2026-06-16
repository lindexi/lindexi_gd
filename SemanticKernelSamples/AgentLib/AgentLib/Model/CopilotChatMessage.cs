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

/// <summary>
/// 表示 Copilot 聊天中的一条消息，支持文本、图片、音频等多模态内容，
/// 以及工具调用、子代理调用和审批等交互。
/// </summary>
public sealed class CopilotChatMessage : NotifyBase, ICopilotChatCurrentContent
{
    /// <summary>
    /// 使用指定角色和纯文本内容创建消息。
    /// </summary>
    /// <param name="role">消息角色。</param>
    /// <param name="content">消息文本内容。</param>
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

    /// <summary>
    /// 克隆构造，用于深拷贝已有消息。
    /// </summary>
    private CopilotChatMessage(ChatRole role, DateTimeOffset createdTime, string timeText, bool isPresetInfo)
    {
        Role = role;
        CreatedTime = createdTime;
        TimeText = timeText;
        IsPresetInfo = isPresetInfo;
        MessageItems.CollectionChanged += MessageItems_CollectionChanged;
    }

    internal static ICopilotChatMessageItem CreateDataItem(DataContent dataContent)
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

    /// <summary>
    /// 从函数调用结果中提取 <see cref="DataContent"/> 并创建对应的消息片段。
    /// 如果结果不是 <see cref="DataContent"/> 或数据为空，则返回 <see langword="null"/>。
    /// </summary>
    internal static ICopilotChatMessageItem? CreateDataItemFromResult(FunctionResultContent functionResultContent)
    {
        if (functionResultContent.Result is DataContent dataContent && dataContent.Data is { Length: > 0 })
        {
            return CreateDataItem(dataContent);
        }

        return null;
    }

    /// <summary>
    /// 消息的角色（用户、助手、系统等）。
    /// </summary>
    public ChatRole Role { get; }

    /// <summary>
    /// 是否预设的内容，预设内容不参与 GPT 信息
    /// </summary>
    public bool IsPresetInfo { set; get; }

    /// <summary>
    /// 消息的作者显示名称，根据角色自动生成（系统/我/Copilot）。
    /// </summary>
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

    /// <summary>
    /// 消息中的内容片段集合，可包含文本、图片、音频、工具调用等多种类型。
    /// </summary>
    public ObservableCollection<ICopilotChatMessageItem> MessageItems { get; } = [];

    /// <summary>
    /// 消息中所有文本片段的拼接内容。
    /// </summary>
    public string Content => string.Concat(MessageItems.OfType<CopilotChatTextItem>().Select(item => item.Text));

    /// <summary>
    /// 消息中所有推理（思考）片段的拼接内容。
    /// </summary>
    public string Reason => string.Concat(MessageItems.OfType<CopilotChatReasoningItem>().Select(item => item.Text));

    /// <summary>
    /// 是否有文本内容。
    /// </summary>
    public bool HasContent => !string.IsNullOrEmpty(Content);

    /// <summary>
    /// 是否有推理内容。
    /// </summary>
    public bool HasReason => !string.IsNullOrEmpty(Reason);

    /// <summary>
    /// 是否同时有推理和文本内容。
    /// </summary>
    public bool HasReasonAndContent => HasReason && HasContent;

    /// <summary>
    /// 会话累计 Token 用量详情。
    /// </summary>
    public UsageDetails? TotalUsageDetails
    {
        get => _totalUsageDetails;
        private set
        {
            if (ReferenceEquals(value, _totalUsageDetails))
            {
                return;
            }

            _totalUsageDetails = value;
            OnUsageDetailsChanged();
        }
    }

    private UsageDetails? _totalUsageDetails;

    /// <summary>
    /// 最新一次响应的 Token 用量详情（不累加，每次直接替换）。
    /// </summary>
    public UsageDetails? CurrentUsageDetails
    {
        get => _currentUsageDetails;
        private set
        {
            if (ReferenceEquals(value, _currentUsageDetails))
            {
                return;
            }

            _currentUsageDetails = value;
            OnPropertyChanged(nameof(CurrentUsageDetails));
        }
    }

    private UsageDetails? _currentUsageDetails;

    private const string UsageSummarySeparator = " ";

    /// <summary>
    /// 是否有 Token 用量详情。
    /// </summary>
    public bool HasUsageDetails => TotalUsageDetails is not null;

    /// <summary>
    /// 是否有总 Token 数。
    /// </summary>
    public bool HasTotalTokenCount => TotalUsageDetails?.TotalTokenCount is not null;

    /// <summary>
    /// 总 Token 数的显示文本。
    /// </summary>
    public string TotalTokenCountText => TotalUsageDetails?.TotalTokenCount is { } totalTokenCount
        ? $"总计 {totalTokenCount:N0}"
        : string.Empty;

    /// <summary>
    /// 是否有输入 Token 数。
    /// </summary>
    public bool HasInputTokenCount => TotalUsageDetails?.InputTokenCount is not null;

    /// <summary>
    /// 输入 Token 数的显示文本。
    /// </summary>
    public string InputTokenCountText
    {
        get
        {
            if (TotalUsageDetails?.InputTokenCount is { } inputTokenCount)
            {
                return $"输入 {inputTokenCount:N0}";
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// 是否有输出 Token 数。
    /// </summary>
    public bool HasOutputTokenCount => TotalUsageDetails?.OutputTokenCount is not null;

    /// <summary>
    /// 输出 Token 数的显示文本。
    /// </summary>
    public string OutputTokenCountText
    {
        get
        {
            if (TotalUsageDetails?.OutputTokenCount is { } outputTokenCount)
            {
                return $"输出 {outputTokenCount:N0}";
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// 是否有推理 Token 数。
    /// </summary>
    public bool HasReasoningTokenCount => TotalUsageDetails?.ReasoningTokenCount is not null;

    /// <summary>
    /// 推理 Token 数的显示文本。
    /// </summary>
    public string ReasoningTokenCountText => TotalUsageDetails?.ReasoningTokenCount is { } reasoningTokenCount
        ? $"思考 {reasoningTokenCount:N0}"
        : string.Empty;

    /// <summary>
    /// 是否有缓存输入 Token 数。
    /// </summary>
    public bool HasCachedInputTokenCount => TotalUsageDetails?.CachedInputTokenCount is not null;

    /// <summary>
    /// 缓存输入 Token 数的显示文本。
    /// </summary>
    public string CachedInputTokenCountText => TotalUsageDetails?.CachedInputTokenCount is { } cachedInputTokenCount
        ? $"缓存 {cachedInputTokenCount:N0}"
        : string.Empty;

    /// <summary>
    /// Token 用量摘要文本，组合所有可用的用量信息。
    /// </summary>
    public string UsageSummaryText
    {
        get
        {
            if (TotalUsageDetails is null)
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

    /// <summary>
    /// 消息的完整内容，包含所有片段（文本、工具调用、子代理等）的格式化文本。
    /// </summary>
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

    /// <summary>
    /// 消息创建时间。
    /// </summary>
    public DateTimeOffset CreatedTime { get; }

    /// <summary>
    /// 消息创建时间的显示文本（HH:mm 格式）。
    /// </summary>
    public string TimeText { get; }

    /// <summary>
    /// 创建当前消息的深拷贝，包含所有 MessageItems 的递归深拷贝。
    /// 如果存在 <see cref="TotalUsageDetails"/> 或 <see cref="CurrentUsageDetails"/>，则将其引用复制到新实例。
    /// </summary>
    /// <returns>深拷贝后的新消息实例。</returns>
    public CopilotChatMessage Clone()
    {
        var clone = new CopilotChatMessage(Role, CreatedTime, TimeText, IsPresetInfo);
        if (TotalUsageDetails is { } totalUsageDetails)
        {
            clone.TotalUsageDetails = totalUsageDetails;
        }

        if (CurrentUsageDetails is { } currentUsageDetails)
        {
            clone.CurrentUsageDetails = currentUsageDetails;
        }

        foreach (ICopilotChatMessageItem item in MessageItems)
        {
            clone.MessageItems.Add(item.Clone());
        }

        return clone;
    }

    /// <summary>
    /// 创建一条用户消息。
    /// </summary>
    /// <param name="content">消息文本内容。</param>
    /// <returns>创建的用户消息。</returns>
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

    /// <summary>
    /// 创建一条助手消息。
    /// </summary>
    /// <param name="content">消息文本内容。</param>
    /// <param name="isPresetInfo">是否为预设信息，预设信息不参与对话上下文。</param>
    /// <returns>创建的助手消息。</returns>
    public static CopilotChatMessage CreateAssistant(string content, bool isPresetInfo)
    {
        return new CopilotChatMessage(ChatRole.Assistant, content)
        {
            IsPresetInfo = isPresetInfo
        };
    }

    /// <summary>
    /// 从响应内容中提取并追加 Token 用量信息。
    /// </summary>
    /// <param name="contents">AI 响应内容集合。</param>
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

    /// <summary>
    /// 将消息转换为 <see cref="ChatMessage"/> 格式，用于发送给 AI 模型。
    /// </summary>
    /// <returns>转换后的聊天消息。</returns>
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

    /// <summary>
    /// 清空消息中的所有内容片段。
    /// </summary>
    public void ClearMessageItems()
    {
        foreach (INotifyPropertyChanged messageItem in MessageItems.OfType<INotifyPropertyChanged>())
        {
            messageItem.PropertyChanged -= MessageItem_PropertyChanged;
        }

        MessageItems.Clear();
        _toolItemsByCallId.Clear();
        _subAgentItemsByCallId.Clear();
        _invokeSubAgentCallIds.Clear();
    }

    /// <summary>
    /// 追加文本内容到消息中。如果最后一个片段是文本，则追加到该片段；否则创建新文本片段。
    /// </summary>
    /// <param name="text">要追加的文本。</param>
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

    /// <summary>
    /// 追加推理（思考）内容到消息中。
    /// </summary>
    /// <param name="text">要追加的推理文本。</param>
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

    /// <summary>
    /// 注册一个需要人工审批的工具。
    /// </summary>
    /// <param name="toolName">工具名称。</param>
    /// <param name="approvalDescription">审批说明，可为 <see langword="null"/>。</param>
    public void RegisterApprovalTool(string toolName, string? approvalDescription = null)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        _approvalToolDescriptions[toolName] = approvalDescription;
    }

    /// <summary>
    /// 追加函数调用内容到消息中。
    /// </summary>
    /// <param name="functionCallContent">函数调用内容。</param>
    public void AppendFunctionCall(FunctionCallContent functionCallContent)
    {
        ArgumentNullException.ThrowIfNull(functionCallContent);

        if (string.Equals(functionCallContent.Name, "InvokeSubAgent", StringComparison.Ordinal))
        {
            string invokeCallId = string.IsNullOrWhiteSpace(functionCallContent.CallId)
                ? Guid.NewGuid().ToString("N")
                : functionCallContent.CallId;
            _invokeSubAgentCallIds.Add(invokeCallId);
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
            toolItem = new CopilotChatToolItem(callId, functionCallContent.Name, CopilotChatMessageItemFormatter.FormatArgumentsToHumans(functionCallContent));
            _toolItemsByCallId[callId] = toolItem;
            MessageItems.Add(toolItem);
            return;
        }

        toolItem.ToolName = functionCallContent.Name;
        toolItem.InputText = CopilotChatMessageItemFormatter.FormatArgumentsToHumans(functionCallContent) ?? string.Empty;
    }

    /// <summary>
    /// 追加函数调用结果到消息中。
    /// </summary>
    /// <param name="functionResultContent">函数调用结果内容。</param>
    public void AppendFunctionResult(FunctionResultContent functionResultContent)
    {
        ArgumentNullException.ThrowIfNull(functionResultContent);

        string callId = functionResultContent.CallId ?? string.Empty;

        if (_invokeSubAgentCallIds.Contains(callId))
        {
            return;
        }

        if (_subAgentItemsByCallId.ContainsKey(callId))
        {
            AppendSubAgentResult(functionResultContent);
            return;
        }

        if (_approvalToolItemsByCallId.ContainsKey(callId))
        {
            AppendApprovalToolResult(functionResultContent);
            return;
        }

        string resolvedCallId = string.IsNullOrWhiteSpace(functionResultContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionResultContent.CallId;

        if (!_toolItemsByCallId.TryGetValue(resolvedCallId, out CopilotChatToolItem? toolItem))
        {
            toolItem = new CopilotChatToolItem(resolvedCallId, "工具", null);
            _toolItemsByCallId[resolvedCallId] = toolItem;
            MessageItems.Add(toolItem);
        }

        toolItem.OutputText = CopilotChatMessageItemFormatter.FormatArgumentsToHumans(functionResultContent) ?? string.Empty;

        if (CreateDataItemFromResult(functionResultContent) is { } dataItem)
        {
            MessageItems.Add(dataItem);
        }
    }

    /// <summary>
    /// 创建或获取审批工具项。
    /// </summary>
    /// <param name="toolName">工具名称。</param>
    /// <param name="inputText">工具输入文本。</param>
    /// <param name="approvalDescription">审批说明。</param>
    /// <param name="callId">调用 ID，如果为空则自动生成。</param>
    /// <returns>审批工具项。</returns>
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

    /// <summary>
    /// 追加 Token 用量详情。同时累加到 <see cref="TotalUsageDetails"/> 并替换 <see cref="CurrentUsageDetails"/>。
    /// </summary>
    /// <param name="details">用量详情。</param>
    public void AppendUsageDetails(UsageDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        // 每次都独立创建 CurrentUsageDetails，直接替换（不累加）
        var current = new UsageDetails();
        current.Add(details);
        CurrentUsageDetails = current;

        if (TotalUsageDetails is null)
        {
            var total = new UsageDetails();
            total.Add(details);
            TotalUsageDetails = total;
            return;
        }

        TotalUsageDetails.Add(details);
        OnUsageDetailsChanged();
    }

    /// <summary>
    /// 创建或获取子代理项。
    /// </summary>
    /// <param name="toolName">工具名称。</param>
    /// <param name="inputText">输入文本。</param>
    /// <param name="callId">调用 ID，如果为空则自动生成。</param>
    /// <returns>子代理项。</returns>
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
        OnPropertyChanged(nameof(TotalUsageDetails));
        OnPropertyChanged(nameof(CurrentUsageDetails));
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
    private readonly HashSet<string> _invokeSubAgentCallIds = new(StringComparer.Ordinal);

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

    private void AppendSubAgentResult(FunctionResultContent functionResultContent)
    {
        string callId = string.IsNullOrWhiteSpace(functionResultContent.CallId)
            ? Guid.NewGuid().ToString("N")
            : functionResultContent.CallId;

        if (_subAgentItemsByCallId.TryGetValue(callId, out CopilotChatSubAgentItem? subAgentItem))
        {
            subAgentItem.OutputText = CopilotChatMessageItemFormatter.FormatResult(functionResultContent) ?? string.Empty;

            if (CreateDataItemFromResult(functionResultContent) is { } dataItem)
            {
                subAgentItem.MessageItems.Add(dataItem);
            }
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
