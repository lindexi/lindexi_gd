using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace AvaloniaAgentLib.Model;

public sealed class CopilotChatMessage : NotifyBase
{
    public CopilotChatMessage(ChatRole role, string content)
    {
        Role = role;
        Reason = string.Empty;
        Content = content;
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

    public string Content
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(HasReasonAndContent));
            OnPropertyChanged(nameof(FullContent));
        }
    }

    public string Reason
    {
        get;
        set
        {
            if (value == field) return;
            field = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasReason));
            OnPropertyChanged(nameof(HasReasonAndContent));
            OnPropertyChanged(nameof(FullContent));
        }
    }

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

            return $"本次用量 {string.Join(UsageSummarySeparator, parts)}";
        }
    }

    public string FullContent
    {
        get
        {
            if (!HasReason)
            {
                return Content;
            }

            if (!HasContent)
            {
                return $"思考：{Environment.NewLine}{Reason}";
            }

            return $"思考：{Environment.NewLine}{Reason}{Environment.NewLine}--------{Environment.NewLine}{Content}";
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

    private void AppendUsageDetails(UsageContent usageContent)
    {
        if (Role != ChatRole.Assistant || usageContent.Details is null)
        {
            return;
        }

        if (UsageDetails is null)
        {
            var usageDetails = new UsageDetails();
            usageDetails.Add(usageContent.Details);
            UsageDetails = usageDetails;
            return;
        }

        UsageDetails.Add(usageContent.Details);
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
}