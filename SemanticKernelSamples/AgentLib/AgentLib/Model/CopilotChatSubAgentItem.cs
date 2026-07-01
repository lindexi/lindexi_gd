using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

using Microsoft.Extensions.AI;

namespace AgentLib.Model;

/// <summary>
/// 表示子智能体调用片段。
/// </summary>
public sealed class CopilotChatSubAgentItem : NotifyBase, ICopilotChatMessageItem, ICopilotChatCurrentContent
{
    /// <summary>
    /// 创建子智能体调用片段。
    /// </summary>
    /// <param name="callId">调用 ID。</param>
    /// <param name="toolName">子智能体名称。</param>
    /// <param name="inputText">输入文本。</param>
    /// <param name="outputText">输出文本。</param>
    public CopilotChatSubAgentItem(string callId, string toolName, string? inputText, string? outputText = null)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "子智能体" : toolName;
        InputText = inputText ?? string.Empty;
        OutputText = outputText ?? string.Empty;
        MessageItems.CollectionChanged += MessageItems_CollectionChanged;
    }

    /// <summary>
    /// 调用 ID。
    /// </summary>
    public string CallId { get; }

    /// <summary>
    /// 子智能体名称。
    /// </summary>
    public string ToolName
    {
        get => _toolName;
        internal set
        {
            string normalizedValue = string.IsNullOrWhiteSpace(value) ? "子智能体" : value;
            if (!SetField(ref _toolName, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayName));
        }
    }

    private string _toolName = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName => ToolName;

    /// <summary>
    /// 输入文本。
    /// </summary>
    public string InputText
    {
        get => _inputText;
        internal set
        {
            string normalizedValue = value ?? string.Empty;
            if (!SetField(ref _inputText, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasInputText));
        }
    }

    private string _inputText = string.Empty;

    /// <summary>
    /// 是否有输入文本。
    /// </summary>
    public bool HasInputText => !string.IsNullOrEmpty(InputText);

    /// <summary>
    /// 输出文本。
    /// </summary>
    public string OutputText
    {
        get => _outputText;
        internal set
        {
            string normalizedValue = value ?? string.Empty;
            if (!SetField(ref _outputText, normalizedValue))
            {
                return;
            }

            OnPropertyChanged(nameof(HasOutputText));
        }
    }

    private string _outputText = string.Empty;

    /// <summary>
    /// 是否有输出文本。
    /// </summary>
    public bool HasOutputText => !string.IsNullOrEmpty(OutputText);

    /// <summary>
    /// 子智能体内部的消息片段集合。
    /// </summary>
    public ObservableCollection<ICopilotChatMessageItem> MessageItems { get; } = [];

    /// <summary>
    /// 是否有内部消息片段。
    /// </summary>
    public bool HasMessageItems => MessageItems.Count > 0;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void RegisterApprovalTool(string toolName, string? approvalDescription = null)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return;
        }

        _approvalToolDescriptions[toolName] = approvalDescription;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

        toolItem.OutputText = CopilotChatMessageItemFormatter.FormatResult(functionResultContent) ?? string.Empty;

        if (CopilotChatMessage.CreateDataItemFromResult(functionResultContent) is { } dataItem)
        {
            MessageItems.Add(dataItem);
        }
    }

    /// <inheritdoc/>
    public CopilotChatApprovalToolItem CreateApprovalToolItem(string toolName, string? inputText, string? approvalDescription = null,
        string? callId = null, string? displayName = null)
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
            existingItem.DisplayName = displayName;
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
            pendingItem.DisplayName = displayName;
            return pendingItem;
        }

        var approvalToolItem = new CopilotChatApprovalToolItem(resolvedCallId, normalizedToolName, normalizedInputText, resolvedApprovalDescription, displayName);
        _approvalToolItemsByCallId[resolvedCallId] = approvalToolItem;
        MessageItems.Add(approvalToolItem);
        return approvalToolItem;
    }

    /// <inheritdoc/>
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

            if (CopilotChatMessage.CreateDataItemFromResult(functionResultContent) is { } dataItem)
            {
                subAgentItem.MessageItems.Add(dataItem);
            }
        }
    }

    private readonly Dictionary<string, CopilotChatToolItem> _toolItemsByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CopilotChatApprovalToolItem> _approvalToolItemsByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CopilotChatSubAgentItem> _subAgentItemsByCallId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _approvalToolDescriptions = new(StringComparer.Ordinal);

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
            approvalToolItem.OutputText = CopilotChatMessageItemFormatter.FormatResult(functionResultContent) ?? string.Empty;
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

        OnPropertyChanged(nameof(HasMessageItems));
    }

    private void MessageItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MessageItems));
    }

    /// <inheritdoc/>
    ICopilotChatMessageItem ICopilotChatMessageItem.Clone()
    {
        var clone = new CopilotChatSubAgentItem(CallId, ToolName, InputText, OutputText);
        foreach (ICopilotChatMessageItem item in MessageItems)
        {
            clone.MessageItems.Add(item.Clone());
        }

        return clone;
    }
}