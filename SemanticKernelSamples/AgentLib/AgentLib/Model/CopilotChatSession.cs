using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.ObjectModel;

namespace AgentLib.Model;

public sealed class CopilotChatSession : NotifyBase
{
    private const int MaxTitleLength = 20;
    private string _title = "新会话";
    private bool _hasCustomTitle;
    private AgentSession? _agentSession;
    private string? _serializedAgentSessionState;

    public CopilotChatSession()
        : this(Guid.NewGuid(), DateTimeOffset.Now)
    {
    }

    public CopilotChatSession(Guid sessionId, DateTimeOffset startedTime)
    {
        SessionId = sessionId;
        StartedTime = startedTime;
    }

    public Guid SessionId { get; }

    public DateTimeOffset StartedTime { get; }

    public ObservableCollection<CopilotChatMessage> ChatMessages { get; } = [];

    public AgentSession? AgentSession
    {
        get => _agentSession;
        private set => SetField(ref _agentSession, value);
    }

    public string? SerializedAgentSessionState
    {
        get => _serializedAgentSessionState;
        private set => SetField(ref _serializedAgentSessionState, value);
    }

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

    public string DisplayText => $"{Title} {StartedTime:MM-dd HH:mm}";

    public bool HasSerializedAgentSessionState => !string.IsNullOrWhiteSpace(SerializedAgentSessionState);

    public void AddMessage(CopilotChatMessage chatMessage)
    {
        ArgumentNullException.ThrowIfNull(chatMessage);

        ChatMessages.Add(chatMessage);
        TryUpdateTitle(chatMessage);
    }

    public void SetAgentSession(AgentSession? agentSession, string? serializedAgentSessionState)
    {
        AgentSession = agentSession;
        SerializedAgentSessionState = string.IsNullOrWhiteSpace(serializedAgentSessionState)
            ? null
            : serializedAgentSessionState;
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
