using System.IO;

namespace DeepSeekWpf.Models;

public sealed class AppSettings
{
    public string CachePath { get; set; } = string.Empty;

    public string LogPath { get; set; } = string.Empty;

    public string ModelName { get; set; } = "Mock-DeepSeek-Chat";

    public static AppSettings CreateDefault()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekWpf");

        return new AppSettings
        {
            CachePath = Path.Combine(appDataPath, "Cache"),
            LogPath = Path.Combine(appDataPath, "Logs"),
            ModelName = "Mock-DeepSeek-Chat",
        };
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            CachePath = CachePath,
            LogPath = LogPath,
            ModelName = ModelName,
        };
    }
}
