using System.Text.Json.Serialization;
using DeepSeekWpf.Infrastructure;

namespace DeepSeekWpf.Models;

public sealed class ChatMessage : ObservableObject
{
    private ChatRole _role;
    private string _content = string.Empty;
    private string _thoughtContent = string.Empty;
    private bool _isEditing;
    private string _editingContent = string.Empty;
    private string _editingThoughtContent = string.Empty;

    public Guid Id { get; set; } = Guid.NewGuid();

    public ChatRole Role
    {
        get => _role;
        set
        {
            if (SetProperty(ref _role, value))
            {
                OnPropertyChanged(nameof(IsAssistant));
                OnPropertyChanged(nameof(ShouldShowThoughtSection));
            }
        }
    }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public string ThoughtContent
    {
        get => _thoughtContent;
        set
        {
            if (SetProperty(ref _thoughtContent, value))
            {
                OnPropertyChanged(nameof(HasThoughtContent));
                OnPropertyChanged(nameof(ShouldShowThoughtSection));
            }
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public bool IsAssistant => Role == ChatRole.Assistant;

    [JsonIgnore]
    public bool HasThoughtContent => !string.IsNullOrWhiteSpace(ThoughtContent);

    [JsonIgnore]
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

    [JsonIgnore]
    public string EditingContent
    {
        get => _editingContent;
        set => SetProperty(ref _editingContent, value);
    }

    [JsonIgnore]
    public string EditingThoughtContent
    {
        get => _editingThoughtContent;
        set => SetProperty(ref _editingThoughtContent, value);
    }

    [JsonIgnore]
    public bool ShouldShowThoughtSection => IsAssistant && (IsEditing || HasThoughtContent);
}
