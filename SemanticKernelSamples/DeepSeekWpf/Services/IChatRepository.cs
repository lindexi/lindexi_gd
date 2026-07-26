using System.IO;
using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface IChatRepository
{
    Task<ChatRepositoryLoadResult> LoadSessionsAsync(CancellationToken cancellationToken = default);

    Task SaveSessionAsync(ChatSession session, CancellationToken cancellationToken = default);

    Task DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public sealed record ChatRepositoryLoadResult(
    IReadOnlyList<ChatSession> Sessions,
    IReadOnlyList<ChatLoadIssue> Issues);

public sealed record ChatLoadIssue(
    string FileName,
    string ExceptionType,
    string RecoverySuggestion);

public sealed class StorageException : IOException
{
    public StorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
