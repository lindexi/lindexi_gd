using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
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

    public IReadOnlyList<ChatSession> LoadSessions()
    {
        var filePath = GetStorageFilePath();
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var sessions = JsonSerializer.Deserialize<List<ChatSession>>(json, _serializerOptions);
            return sessions?
                .OrderByDescending(session => session.UpdatedAt)
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SaveSessions(IEnumerable<ChatSession> sessions)
    {
        var filePath = GetStorageFilePath();
        var orderedSessions = sessions
            .OrderByDescending(session => session.UpdatedAt)
            .ToList();

        var json = JsonSerializer.Serialize(orderedSessions, _serializerOptions);
        File.WriteAllText(filePath, json);
    }

    private string GetStorageFilePath()
    {
        var cachePath = _settingsService.CurrentSettings.CachePath;
        Directory.CreateDirectory(cachePath);
        return Path.Combine(cachePath, "chat-sessions.json");
    }
}
