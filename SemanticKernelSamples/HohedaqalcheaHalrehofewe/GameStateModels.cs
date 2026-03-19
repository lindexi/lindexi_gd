using System.ComponentModel;
using System.Runtime.CompilerServices;

using Microsoft.Agents.AI;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace HohedaqalcheaHalrehofewe;

internal sealed record GameSettings(
    string ApiKeyFilePath,
    string DeploymentId,
    string MainWord,
    string SpyWord,
    int PlayerCount,
    int HumanPlayerIndex);

internal sealed record HumanTurnRequest(
    int PlayerIndex,
    string Prompt,
    string EntryKey,
    string EntryHeader,
    string Kind);

internal sealed record TimelineDelta(
    string EntryKey,
    string Header,
    string Text,
    string Kind);

internal static class TimelineKinds
{
    public const string System = "System";
    public const string Reasoning = "Reasoning";
    public const string AiSpeech = "AiSpeech";
    public const string AiVote = "AiVote";
    public const string PlayerSpeech = "PlayerSpeech";
    public const string PlayerVote = "PlayerVote";
}

internal sealed class PlayerRuntime
{
    public PlayerRuntime(int index, bool isHuman, string word, List<ChatMessage> messages)
    {
        Index = index;
        IsHuman = isHuman;
        Word = word;
        Messages = messages;
    }

    public int Index { get; }

    public bool IsHuman { get; }

    public string Word { get; }

    public List<ChatMessage> Messages { get; }
}

internal sealed class GameSession
{
    public GameSession(GameSettings settings, ChatClientAgent agent, List<PlayerRuntime> players)
    {
        Settings = settings;
        Agent = agent;
        Players = players;
    }

    public GameSettings Settings { get; }

    public ChatClientAgent Agent { get; }

    public List<PlayerRuntime> Players { get; }

    public int CurrentRound { get; set; }
}

internal abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

internal sealed class PlayerSeat : ObservableObject
{
    private string _statusText = "等待开始";
    private bool _isCurrent;

    public PlayerSeat(int index, bool isHuman, string word)
    {
        Index = index;
        IsHuman = isHuman;
        Word = word;
    }

    public int Index { get; }

    public bool IsHuman { get; }

    public string Word { get; }

    public string DisplayName => $"第 {Index} 人";

    public string ControllerText => IsHuman ? "玩家参与" : "AI 托管";

    public string RoleBadgeText => IsHuman ? "PLAYER" : "AI";

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set => SetProperty(ref _isCurrent, value);
    }
}

internal sealed class TimelineEntry : ObservableObject
{
    private string _content = string.Empty;

    public TimelineEntry(string header, string kind)
    {
        Header = header;
        Kind = kind;
    }

    public string Header { get; }

    public string Kind { get; }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public void Append(string text)
    {
        Content += text;
    }
}
