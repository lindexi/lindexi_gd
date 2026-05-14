using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace AvaloniaAgentLib.Model;

public sealed class CopilotChatMessage : NotifyBase
{
    public CopilotChatMessage(ChatRole role, string content)
    {
        Role = role;
        MessageItems.CollectionChanged += MessageItems_CollectionChanged;

        if (!string.IsNullOrEmpty(content))
        {
            MessageItems.Add(new CopilotChatTextItem(content));
        }

        CreatedTime = DateTimeOffset.Now;
        TimeText = CreatedTime.ToString("HH:mm");
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

            return "";
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

    public static CopilotChatMessage CreateAssistant(string content, bool isPresetInfo)
    {
        return new CopilotChatMessage(ChatRole.Assistant, content)
        {
            IsPresetInfo = isPresetInfo
        };
    }

    internal void AppendUsageDetails(IEnumerable<AIContent>? contents)
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
        var chatMessage = this;
        return new ChatMessage(chatMessage.Role, chatMessage.Content);
    }

    internal void ClearMessageItems()
    {
        foreach (INotifyPropertyChanged messageItem in MessageItems.OfType<INotifyPropertyChanged>())
        {
            messageItem.PropertyChanged -= MessageItem_PropertyChanged;
        }

        MessageItems.Clear();
        _toolItemsByCallId.Clear();
    }

    internal void AppendText(string text)
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

    internal void AppendReasoning(string text)
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

    internal void AppendFunctionCall(FunctionCallContent functionCallContent)
    {
        ArgumentNullException.ThrowIfNull(functionCallContent);

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

    internal void AppendFunctionResult(FunctionResultContent functionResultContent)
    {
        ArgumentNullException.ThrowIfNull(functionResultContent);

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

    private void AppendUsageDetails(UsageContent usageContent)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Role != ChatRole.Assistant || usageContent.Details is null)
        {
            return;
        }

        AppendUsageDetails(usageContent.Details);
    }

    public void AppendUsageDetails(UsageDetails details)
    {
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

    private static string? FormatMessageItem(ICopilotChatMessageItem messageItem)
    {
        return messageItem switch
        {
            CopilotChatTextItem textItem => textItem.Text,
            CopilotChatReasoningItem reasoningItem => $"思考：{Environment.NewLine}{reasoningItem.Text}",
            CopilotChatToolItem toolItem => FormatToolItem(toolItem),
            _ => null
        };
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