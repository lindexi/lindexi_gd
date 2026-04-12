using DeepSeekWpf.Infrastructure;

namespace DeepSeekWpf.Models;

public sealed class ChatMessage : ObservableObject
{
    private string _content = string.Empty;

    public Guid Id { get; set; } = Guid.NewGuid();

    public ChatRole Role { get; set; }

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
