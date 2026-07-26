using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface IAiChatService
{
    IAsyncEnumerable<AiResponseChunk> GetReplyAsync(
        ChatSession session,
        ChatMessageViewModel assistantMessage,
        AppSettings settings,
        CancellationToken cancellationToken);
}
