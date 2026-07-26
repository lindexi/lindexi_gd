using System.IO;

namespace DeepSeekWpf.Models;

public sealed record AppSettings
{
    public string CachePath { get; init; } = string.Empty;

    public string DataPath { get; init; } = string.Empty;

    public string LogPath { get; init; } = string.Empty;

    public string ModelName { get; init; } = "Mock-DeepSeek-Chat";

    public string ApiAddress { get; init; } = "https://api.example.com/v1/chat/completions";

    public string ApiKey { get; init; } = string.Empty;

    public static AppSettings CreateDefault()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekWpf");

        return new AppSettings
        {
            CachePath = Path.Combine(appDataPath, "Cache"),
            DataPath = Path.Combine(appDataPath, "Data"),
            LogPath = Path.Combine(appDataPath, "Logs"),
            ModelName = "Mock-DeepSeek-Chat",
            ApiAddress = "https://api.example.com/v1/chat/completions",
            ApiKey = string.Empty,
        };
    }
}
