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
    public CopilotChatSubAgentItem(string callId, string toolName, string? inputText, string? outputText = null)
    {
        CallId = callId;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? "子智能体" : toolName;
        InputText = inputText ?? string.Empty;
        OutputText = outputText ?? string.Empty;
        MessageItems.CollectionChanged += MessageItems_CollectionChanged;
    }

    public string CallId { get; }

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

    public string DisplayName => ToolName;

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

    public bool HasInputText => !string.IsNullOrEmpty(InputText);

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

    public bool HasOutputText => !string.IsNullOrEmpty(OutputText);

    public ObservableCollection<ICopilotChatMessageItem> MessageItems { get; } = [];

    public bool HasMessageItems => MessageItems.Count > 0;

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

        toolItem.OutputText = CopilotChatMessageItemFormatter.FormatResult(functionResultContent) ?? string.Empty;
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
}