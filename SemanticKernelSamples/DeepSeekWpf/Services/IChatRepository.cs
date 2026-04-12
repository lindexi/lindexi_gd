using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface IChatRepository
{
    IReadOnlyList<ChatSession> LoadSessions();

    void SaveSessions(IEnumerable<ChatSession> sessions);
}
