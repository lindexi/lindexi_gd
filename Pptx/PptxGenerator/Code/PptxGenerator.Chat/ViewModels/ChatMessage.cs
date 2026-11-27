using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PptxGenerator.Chat.Services;

namespace PptxGenerator.Chat.ViewModels;

internal abstract class Notify : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        return true;
    }

    protected void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed class ChatMessage : Notify
{
    private string _text = string.Empty;
    private string _status = string.Empty;
    private ChatUsage _usage;
    private string _usageSummary = string.Empty;

    internal ChatMessage(string role, bool isUser, string text, IReadOnlyList<ChatImage> images)
    {
        Role = role;
        IsUser = isUser;
        Images = images;
        if (text.Length > 0) AppendText(0, text);
    }

    public string Role { get; }
    public bool IsUser { get; }
    public string Text { get => _text; internal set => Set(ref _text, value); }
    public string Status { get => _status; internal set => Set(ref _status, value); }
    public string UsageSummary { get => _usageSummary; private set => Set(ref _usageSummary, value); }
    public IReadOnlyList<ChatImage> Images { get; }
    public ObservableCollection<string> Notices { get; } = new();
    public ObservableCollection<ChatPart> Parts { get; } = new();

    internal SessionMessage Snapshot() => new(IsUser, Text, Status, UsageSummary, Images.ToArray(), Notices.ToArray(),
        Parts.Select(part => part is ChatTextPart text ? new SessionPart(part.Index, text.Text, null) :
            new SessionPart(part.Index, null, ((ChatToolPart)part).Activity)).ToArray());

    internal static ChatMessage Restore(SessionMessage saved)
    {
        var message = new ChatMessage(UiText.Get(saved.IsUser ? "UserRole" : "AssistantRole"), saved.IsUser,
            string.Empty, saved.Images ?? Array.Empty<ChatImage>())
        {
            Status = saved.Status ?? string.Empty,
            UsageSummary = saved.UsageSummary ?? string.Empty,
        };
        foreach (var part in saved.Parts ?? Array.Empty<SessionPart>())
        {
            if (part?.Text is { } text) message.AppendText(part.Index, text);
            else if (part?.Tool is { } tool) message.UpdateTool(part.Index, tool);
        }
        if (message.Text.Length == 0 && saved.Text is { Length: > 0 } fallback) message.AppendText(0, fallback);
        foreach (var notice in saved.Notices ?? Array.Empty<string>()) message.Notices.Add(notice);
        return message;
    }

    internal void AddUsage(OpenAI.Responses.ResponseTokenUsage usage)
    {
        _usage = _usage.Add(usage);
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var summary = string.Format(culture, UiText.Get("UsageSummary"), _usage.Total, _usage.Input, _usage.Output);
        if (_usage.Reasoning is { } reasoning) summary += UiText.Format("UsageReasoning", reasoning.ToString("N0", culture));
        if (_usage.Cached is { } cached) summary += UiText.Format("UsageCached", cached.ToString("N0", culture));
        UsageSummary = summary;
    }

    internal void AppendText(int index, string delta)
    {
        if (delta.Length == 0) return;
        var part = GetOrInsert(index, () => new ChatTextPart(index));
        part.Text += delta;
        Text = string.Join("\n\n", Parts.OfType<ChatTextPart>().Select(item => item.Text));
    }

    internal void UpdateTool(int index, ToolActivity activity) =>
        GetOrInsert(index, () => new ChatToolPart(index)).Activity = activity;

    private T GetOrInsert<T>(int index, Func<T> create) where T : ChatPart
    {
        int position = 0;
        while (position < Parts.Count && Parts[position].Index < index) position++;
        if (position < Parts.Count && Parts[position] is T existing && existing.Index == index) return existing;
        var part = create();
        Parts.Insert(position, part);
        return part;
    }
}

internal abstract class ChatPart : Notify
{
    protected ChatPart(int index) => Index = index;
    internal int Index { get; }
}

internal sealed class ChatTextPart : ChatPart
{
    private string _text = string.Empty;
    internal ChatTextPart(int index) : base(index) { }
    public string Text { get => _text; internal set => Set(ref _text, value); }
}

internal sealed class ChatToolPart : ChatPart
{
    private ToolActivity? _activity;
    internal ChatToolPart(int index) : base(index) { }
    public ToolActivity? Activity { get => _activity; internal set => Set(ref _activity, value); }
}
