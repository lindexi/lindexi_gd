using System.Collections.ObjectModel;
using System.Linq;
using DeepSeekWpf.Infrastructure;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Models;

public sealed class ChatSession : ObservableObject
{
    private string _title = "新对话";
    private DateTime _updatedAt = DateTime.Now;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

    public bool IsEmpty => Messages.Count == 0;

    public void Touch()
    {
        UpdatedAt = DateTime.Now;
    }

    public void RefreshTitleFromMessages()
    {
        var firstUserMessage = Messages.FirstOrDefault(message => message.Role == ChatRole.User);

        Title = firstUserMessage switch
        {
            null => "新对话",
            { Content.Length: > 0 } => BuildTitle(firstUserMessage.Content),
            { ImageAttachments.Count: > 0 } => $"图片：{firstUserMessage.ImageAttachments[0].FileName}",
            _ => "新对话",
        };
    }

    private static string BuildTitle(string content)
    {
        var normalized = content.Trim().Replace(Environment.NewLine, " ");
        return normalized.Length <= 20 ? normalized : normalized[..20] + "...";
    }
}
