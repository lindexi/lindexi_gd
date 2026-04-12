using System.Collections.ObjectModel;
using System.Linq;
using DeepSeekWpf.Infrastructure;

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

    public ObservableCollection<ChatMessage> Messages { get; set; } = [];

    public void Touch()
    {
        UpdatedAt = DateTime.Now;
    }

    public void RefreshTitleFromMessages()
    {
        var firstUserMessage = Messages.FirstOrDefault(message =>
            message.Role == ChatRole.User &&
            !string.IsNullOrWhiteSpace(message.Content));

        Title = firstUserMessage is null
            ? "新对话"
            : BuildTitle(firstUserMessage.Content);
    }

    private static string BuildTitle(string content)
    {
        var normalized = content.Trim().Replace(Environment.NewLine, " ");
        return normalized.Length <= 20 ? normalized : normalized[..20] + "...";
    }
}
