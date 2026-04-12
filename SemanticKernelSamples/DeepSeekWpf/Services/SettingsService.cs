using System.Text.Json;
using System.IO;
using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsFilePath;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekWpf");

        Directory.CreateDirectory(appDataPath);
        _settingsFilePath = Path.Combine(appDataPath, "settings.json");
        CurrentSettings = LoadSettings();
        EnsureDirectories(CurrentSettings);
        Persist(CurrentSettings);
    }

    public AppSettings CurrentSettings { get; private set; }

    public void Save(AppSettings settings)
    {
        CurrentSettings = settings.Clone();
        EnsureDirectories(CurrentSettings);
        Persist(CurrentSettings);
    }

    public void RestoreDefaults()
    {
        Save(AppSettings.CreateDefault());
    }

    private AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions);
            return settings ?? AppSettings.CreateDefault();
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    private void Persist(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, _serializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private static void EnsureDirectories(AppSettings settings)
    {
        Directory.CreateDirectory(settings.CachePath);
        Directory.CreateDirectory(settings.LogPath);
    }
}
