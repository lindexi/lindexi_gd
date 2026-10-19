using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AgentLib.Model;
using Microsoft.Extensions.AI;

namespace PrismCodingAgent.AvaloniaShell.ViewModels;

/// <summary>
/// 将一条 <see cref="CopilotChatMessage"/> 投影为聊天界面可绑定的消息项。
/// </summary>
public sealed class MessageItemViewModel : ViewModelBase, IDisposable
{
    private readonly CopilotChatMessage _message;
    private bool _isDisposed;

    /// <summary>
    /// 使用指定消息创建显示投影。
    /// </summary>
    /// <param name="message">要投影的聊天消息。</param>
    public MessageItemViewModel(CopilotChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _message = message;
        _message.PropertyChanged += OnMessagePropertyChanged;
        Localizations.Lang.Current.PropertyChanged += OnLanguageChanged;
    }

    /// <summary>
    /// 获取底层聊天消息。
    /// </summary>
    public CopilotChatMessage Message => _message;

    /// <summary>
    /// 获取作者显示名。
    /// </summary>
    public string Author => _message.Author;

    /// <summary>
    /// 获取公开正文。
    /// </summary>
    public string Content => _message.Content;

    /// <summary>
    /// 获取包含思考、工具与子代理片段的完整内容。
    /// </summary>
    public string FullContent => _message.FullContent;

    /// <summary>
    /// 获取消息片段集合。
    /// </summary>
    public ObservableCollection<ICopilotChatMessageItem> MessageItems => _message.MessageItems;

    /// <summary>
    /// 获取消息时间文本。
    /// </summary>
    public string TimeText => _message.TimeText;

    /// <summary>
    /// 获取是否为用户消息。
    /// </summary>
    public bool IsUserMessage => _message.Role == ChatRole.User;

    /// <summary>
    /// 获取是否为 Copilot 消息。
    /// </summary>
    public bool IsAssistantMessage => _message.Role == ChatRole.Assistant;

    /// <summary>
    /// 获取是否为系统消息。
    /// </summary>
    public bool IsSystemMessage => _message.Role == ChatRole.System;

    /// <summary>
    /// 获取是否有 Token 用量详情。
    /// </summary>
    public bool HasUsageDetails => _message.HasUsageDetails;

    /// <summary>
    /// 获取 Token 用量摘要。
    /// </summary>
    public string UsageSummaryText
    {
        get
        {
            var parts = new System.Collections.Generic.List<string>();
            var usage = _message.TotalUsageDetails;
            if (usage?.TotalTokenCount is { } total) parts.Add(Localizations.Lang.Format(Localizations.Lang.Current.UsageTotal, total));
            if (usage?.InputTokenCount is { } input) parts.Add(Localizations.Lang.Format(Localizations.Lang.Current.UsageInput, input));
            if (usage?.OutputTokenCount is { } output) parts.Add(Localizations.Lang.Format(Localizations.Lang.Current.UsageOutput, output));
            if (usage?.ReasoningTokenCount is { } reasoning) parts.Add(Localizations.Lang.Format(Localizations.Lang.Current.UsageReasoning, reasoning));
            if (usage?.CachedInputTokenCount is { } cached) parts.Add(Localizations.Lang.Format(Localizations.Lang.Current.UsageCached, cached));
            string totalUsageSummaryText = string.Join(" | ", parts);
            if (_message.CurrentUsageDetails?.TotalTokenCount is not { } currentTotalTokenCount)
            {
                return totalUsageSummaryText;
            }

            string currentUsageSummaryText = Localizations.Lang.Format(Localizations.Lang.Current.UsageCurrent, currentTotalTokenCount);
            return string.IsNullOrWhiteSpace(totalUsageSummaryText)
                ? currentUsageSummaryText
                : $"{currentUsageSummaryText} {totalUsageSummaryText}";
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _message.PropertyChanged -= OnMessagePropertyChanged;
        Localizations.Lang.Current.PropertyChanged -= OnLanguageChanged;
        _isDisposed = true;
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(UsageSummaryText));

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CopilotChatMessage.Content):
                OnPropertyChanged(nameof(Content));
                break;
            case nameof(CopilotChatMessage.FullContent):
                OnPropertyChanged(nameof(FullContent));
                break;
            case nameof(CopilotChatMessage.HasUsageDetails):
                OnPropertyChanged(nameof(HasUsageDetails));
                break;
            case nameof(CopilotChatMessage.CurrentUsageDetails):
                OnPropertyChanged(nameof(UsageSummaryText));
                break;
            case nameof(CopilotChatMessage.UsageSummaryText):
                OnPropertyChanged(nameof(UsageSummaryText));
                break;
        }
    }
}
