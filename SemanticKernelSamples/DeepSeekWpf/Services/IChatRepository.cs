using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface IChatRepository
{
    Task<IReadOnlyList<ChatSession>> LoadSessionsAsync(CancellationToken cancellationToken = default);

    void SaveSession(ChatSession session);

    void DeleteSession(Guid sessionId);
}
