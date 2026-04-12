using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface IAiChatService
{
    IAsyncEnumerable<AiResponseChunk> GetReplyAsync(
        ChatSession session,
        ChatMessage assistantMessage,
        AppSettings settings,
        CancellationToken cancellationToken);
}
