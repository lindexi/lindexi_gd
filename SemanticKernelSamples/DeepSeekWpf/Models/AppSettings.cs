using System.IO;
using System.Text.Json.Serialization;

namespace DeepSeekWpf.Models;

public sealed record AppSettings
{
    private string _selectedModelSpecifier = string.Empty;

    public string CachePath { get; init; } = string.Empty;

    public string DataPath { get; init; } = string.Empty;

    public string LogPath { get; init; } = string.Empty;

    public int ChatRequestTimeoutSeconds { get; init; } = 120;

    public string SelectedModelSpecifier
    {
        get => _selectedModelSpecifier;
        init => _selectedModelSpecifier = value ?? string.Empty;
    }

    [JsonPropertyName("ModelName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyModelName
    {
        get => null;
        init
        {
            if (string.IsNullOrWhiteSpace(_selectedModelSpecifier))
            {
                _selectedModelSpecifier = value ?? string.Empty;
            }
        }
    }

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
            ChatRequestTimeoutSeconds = 120,
            SelectedModelSpecifier = string.Empty,
        };
    }
}
