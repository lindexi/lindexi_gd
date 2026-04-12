using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface IAiChatService
{
    Task<string> GetReplyAsync(ChatSession session, AppSettings settings, CancellationToken cancellationToken);
}
