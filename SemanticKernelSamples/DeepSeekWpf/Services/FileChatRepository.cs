using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Linq;
using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public sealed class FileChatRepository : IChatRepository
{
    private readonly ISettingsService _settingsService;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public FileChatRepository(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<ChatSession>> LoadSessionsAsync(CancellationToken cancellationToken = default)
    {
        var storageDirectory = GetStorageDirectory();
        if (!Directory.Exists(storageDirectory))
        {
            return [];
        }

        var files = Directory.EnumerateFiles(storageDirectory, "*.json", SearchOption.TopDirectoryOnly).ToList();
        var loadTasks = files.Select(filePath => LoadSessionAsync(filePath, cancellationToken));
        var sessions = await Task.WhenAll(loadTasks);

        return sessions
            .Where(session => session is not null)
            .Cast<ChatSession>()
            .OrderByDescending(session => session.UpdatedAt)
            .ToList();
    }

    public void SaveSession(ChatSession session)
    {
        var filePath = GetSessionFilePath(session.Id);
        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, session, _serializerOptions);
    }

    public void DeleteSession(Guid sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private async Task<ChatSession?> LoadSessionAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<ChatSession>(stream, _serializerOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private string GetStorageDirectory()
    {
        var dataPath = Path.Combine(_settingsService.CurrentSettings.DataPath, "Sessions");
        Directory.CreateDirectory(dataPath);
        return dataPath;
    }

    private string GetSessionFilePath(Guid sessionId)
    {
        return Path.Combine(GetStorageDirectory(), $"{sessionId}.json");
    }
}
