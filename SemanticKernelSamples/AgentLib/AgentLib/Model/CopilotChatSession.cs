using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.ObjectModel;

namespace AgentLib.Model;

/// <summary>
/// 表示一个 Copilot 聊天会话，包含会话元数据和消息列表。
/// </summary>
public sealed class CopilotChatSession : NotifyBase
{
    private const int MaxTitleLength = 20;
    private string _title = "新会话";
    private bool _hasCustomTitle;
    private AgentSession? _agentSession;

    /// <summary>
    /// 使用新的会话 ID 和当前时间创建会话。
    /// </summary>
    public CopilotChatSession()
        : this(Guid.NewGuid(), DateTimeOffset.Now)
    {
    }

    /// <summary>
    /// 使用指定的会话 ID 和开始时间创建会话。
    /// </summary>
    /// <param name="sessionId">会话唯一标识符。</param>
    /// <param name="startedTime">会话开始时间。</param>
    public CopilotChatSession(Guid sessionId, DateTimeOffset startedTime)
    {
        SessionId = sessionId;
        StartedTime = startedTime;
    }

    /// <summary>
    /// 会话唯一标识符。
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// 会话开始时间。
    /// </summary>
    public DateTimeOffset StartedTime { get; }

    /// <summary>
    /// 会话中的聊天消息列表。
    /// </summary>
    public ObservableCollection<CopilotChatMessage> ChatMessages { get; } = [];

    /// <summary>
    /// 当前会话关联的代理会话。当不携带历史时为 <see langword="null"/>。
    /// </summary>
    public AgentSession? AgentSession
    {
        get => _agentSession;
        private set => SetField(ref _agentSession, value);
    }

    /// <summary>
    /// 会话标题。默认为"新会话"，在收到第一条用户消息后自动生成。
    /// </summary>
    public string Title
    {
        get => _title;
        private set
        {
            if (!SetField(ref _title, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// 用于显示的文本，包含标题和开始时间。
    /// </summary>
    public string DisplayText => $"{Title} {StartedTime:MM-dd HH:mm}";

    /// <summary>
    /// 向会话中添加一条聊天消息，并尝试更新会话标题。
    /// </summary>
    /// <param name="chatMessage">要添加的聊天消息。</param>
    public void AddMessage(CopilotChatMessage chatMessage)
    {
        ArgumentNullException.ThrowIfNull(chatMessage);

        ChatMessages.Add(chatMessage);
        TryUpdateTitle(chatMessage);
    }

    /// <summary>
    /// 设置当前会话关联的代理会话。
    /// </summary>
    /// <param name="agentSession">代理会话，可为 <see langword="null"/>。</param>
    public void SetAgentSession(AgentSession? agentSession)
    {
        AgentSession = agentSession;
    }

    private void TryUpdateTitle(CopilotChatMessage chatMessage)
    {
        if (_hasCustomTitle || chatMessage.Role != ChatRole.User || chatMessage.IsPresetInfo)
        {
            return;
        }

        string title = CreateTitle(chatMessage.Content);
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _hasCustomTitle = true;
        Title = title;
    }

    private static string CreateTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        string title = string.Join(" ", content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (title.Length <= MaxTitleLength)
        {
            return title;
        }

        return $"{title[..MaxTitleLength]}...";
    }
}
