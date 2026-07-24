using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

using AgentLib.Model;

using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

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
    public string UsageSummaryText => _message.UsageSummaryText;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _message.PropertyChanged -= OnMessagePropertyChanged;
        _isDisposed = true;
    }

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
            case nameof(CopilotChatMessage.UsageSummaryText):
                OnPropertyChanged(nameof(UsageSummaryText));
                break;
        }
    }
}
