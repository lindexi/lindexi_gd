using System.IO;

namespace DeepSeekWpf.Models;

public sealed record AppSettings
{
    public string CachePath { get; init; } = string.Empty;

    public string DataPath { get; init; } = string.Empty;

    public string LogPath { get; init; } = string.Empty;

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
        };
    }
}
