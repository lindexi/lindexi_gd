using System.ComponentModel;
using AgentLib.Model;
using DeepSeekWpf.Infrastructure;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Models;

public sealed class ChatMessageViewModel : ObservableObject
{
    private bool _isEditing;
    private string _editingContent = string.Empty;
    private string _editingThoughtContent = string.Empty;

    public ChatMessageViewModel(
        CopilotChatMessage message,
        Guid? id = null,
        DateTime? createdAt = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Id = id ?? Guid.NewGuid();
        CreatedAt = createdAt ?? message.CreatedTime.LocalDateTime;
        Message.PropertyChanged += MessageOnPropertyChanged;
    }

    public Guid Id { get; }

    public CopilotChatMessage Message { get; }

    public ChatRole Role => Message.Role;

    public string Content => Message.Content;

    public string ThoughtContent => Message.Reason;

    public DateTime CreatedAt { get; }

    public bool IsAssistant => Role == ChatRole.Assistant;

    public bool HasThoughtContent => !string.IsNullOrWhiteSpace(ThoughtContent);

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(ShouldShowThoughtSection));
            }
        }
    }

    public string EditingContent
    {
        get => _editingContent;
        set => SetProperty(ref _editingContent, value);
    }

    public string EditingThoughtContent
    {
        get => _editingThoughtContent;
        set => SetProperty(ref _editingThoughtContent, value);
    }

    public bool ShouldShowThoughtSection => IsAssistant && (IsEditing || HasThoughtContent);

    public void ReplaceContent(string content, string thoughtContent)
    {
        Message.ClearMessageItems();
        Message.AppendReasoning(thoughtContent);
        Message.AppendText(content);
    }

    private void MessageOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotChatMessage.Content))
        {
            OnPropertyChanged(nameof(Content));
        }
        else if (e.PropertyName == nameof(CopilotChatMessage.Reason))
        {
            OnPropertyChanged(nameof(ThoughtContent));
            OnPropertyChanged(nameof(HasThoughtContent));
            OnPropertyChanged(nameof(ShouldShowThoughtSection));
        }
    }
}
