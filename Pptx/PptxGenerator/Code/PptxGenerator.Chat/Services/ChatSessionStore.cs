using System.IO;
using System.Text.Json;

namespace PptxGenerator.Chat.Services;

internal sealed record SessionPart(int Index, string? Text, ToolActivity? Tool);
internal sealed record SessionMessage(bool IsUser, string Text, string Status, string UsageSummary,
    ChatImage[] Images, string[] Notices, SessionPart[] Parts);
internal sealed record SessionSummary(Guid Id, string Title, DateTimeOffset UpdatedAt)
{
    public DateTime LocalUpdatedAt => UpdatedAt.LocalDateTime;
}
internal sealed record ChatSession(SessionSummary Summary, SessionMessage[] Messages);

internal sealed class ChatSessionStore
{
    internal static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PptxGenerator.Chat", "Sessions");

    private readonly string _directory;
    internal ChatSessionStore(string directory) => _directory = directory;

    internal async Task SaveAsync(ChatSession session)
    {
        Directory.CreateDirectory(_directory);
        string path = GetPath(session.Summary.Id);
        string temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporary))
                await JsonSerializer.SerializeAsync(stream, session).ConfigureAwait(false);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    internal async Task<(IReadOnlyList<SessionSummary> Sessions, int Unreadable)> ListAsync()
    {
        var sessions = new List<SessionSummary>();
        int unreadable = 0;
        Directory.CreateDirectory(_directory);
        foreach (var path in Directory.EnumerateFiles(_directory, "*.json"))
        {
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id)) continue;
            try
            {
                // Deserializing only the header avoids retaining attachments for the history list.
                await using var stream = File.OpenRead(path);
                var header = await JsonSerializer.DeserializeAsync<SessionHeader>(stream).ConfigureAwait(false);
                if (header?.Summary is not { } summary || summary.Id != id || summary.Title is null)
                    throw new JsonException("Missing or invalid session summary.");
                sessions.Add(summary);
            }
            catch (Exception exception) when (IsStorageError(exception))
            {
                ChatDiagnostics.Failure("ListSession", exception, "SessionPath", path);
                unreadable++;
            }
        }
        return (sessions.OrderByDescending(session => session.UpdatedAt).ToArray(), unreadable);
    }

    internal async Task<ChatSession> LoadAsync(Guid id)
    {
        await using var stream = File.OpenRead(GetPath(id));
        var session = await JsonSerializer.DeserializeAsync<ChatSession>(stream).ConfigureAwait(false);
        if (session?.Summary?.Id != id || session.Messages is null || session.Messages.Length == 0)
            throw new JsonException("Missing or invalid session data.");
        return session;
    }

    internal static bool IsStorageError(Exception exception) => exception is IOException or UnauthorizedAccessException or JsonException;

    private string GetPath(Guid id) => Path.Combine(_directory, $"{id:N}.json");
    private sealed record SessionHeader(SessionSummary Summary);
}
