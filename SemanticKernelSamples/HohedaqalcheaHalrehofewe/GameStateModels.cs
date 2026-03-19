using System.ComponentModel;
using System.Runtime.CompilerServices;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace HohedaqalcheaHalrehofewe;

internal sealed record GameSettings(
    string ApiKeyFilePath,
    string DeploymentId,
    string MainWord,
    string SpyWord,
    int PlayerCount,
    int HumanPlayerIndex);

internal enum TurnStage
{
    None,
    Speaking,
    Voting,
}

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class PlayerSeat : ObservableObject
{
    private string _statusText = "等待开始";

    public PlayerSeat(int index, bool isHuman, string word, List<ChatMessage> messages)
    {
        Index = index;
        IsHuman = isHuman;
        Word = word;
        Messages = messages;
    }

    public int Index { get; }

    public bool IsHuman { get; }

    public string Word { get; }

    public string DisplayName => $"第 {Index} 人";

    public string ControllerText => IsHuman ? "玩家" : "AI";

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public List<ChatMessage> Messages { get; }
}

public sealed class TimelineEntry : ObservableObject
{
    private string _content = string.Empty;

    public TimelineEntry(string header)
    {
        Header = header;
    }

    public string Header { get; }

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
