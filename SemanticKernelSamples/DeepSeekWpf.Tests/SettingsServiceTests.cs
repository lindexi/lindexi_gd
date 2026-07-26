using System.Text.Json;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;
using DeepSeekWpf.Tests.TestInfrastructure;

namespace DeepSeekWpf.Tests;

[TestClass]
public sealed class SettingsServiceTests
{
    [TestMethod]
    public async Task InitializeAsync_MissingFile_WritesIsolatedDefaults()
    {
        using var temp = new TempDirectory();
        var defaults = CreateSettings(temp, "default-model");
        var service = new SettingsService(temp.GetPath("settings.json"), () => defaults);

        await service.InitializeAsync();

        Assert.IsTrue(File.Exists(service.SettingsFilePath));
        Assert.AreEqual(defaults, service.CurrentSettings);
        Assert.IsTrue(Directory.Exists(defaults.CachePath));
        Assert.IsTrue(Directory.Exists(defaults.DataPath));
        Assert.IsTrue(Directory.Exists(defaults.LogPath));
    }

    [TestMethod]
    public async Task LoadAsync_LegacyCredentials_AreNotRetainedAndModelNameMigrates()
    {
        using var temp = new TempDirectory();
        var settingsPath = temp.GetPath("settings.json");
        var defaults = CreateSettings(temp, string.Empty);
        await File.WriteAllTextAsync(settingsPath, $$"""
            {
              "CachePath": "{{defaults.CachePath.Replace("\\", "\\\\")}}",
              "DataPath": "{{defaults.DataPath.Replace("\\", "\\\\")}}",
              "LogPath": "{{defaults.LogPath.Replace("\\", "\\\\")}}",
              "ApiKey": "secret-value",
              "ApiAddress": "https://example.invalid",
              "ModelName": "legacy-model"
            }
            """);
        var service = new SettingsService(settingsPath, () => defaults);

        var loaded = await service.LoadAsync();
        await service.SaveAsync(loaded);
        var saved = await File.ReadAllTextAsync(settingsPath);

        Assert.AreEqual("legacy-model", loaded.SelectedModelSpecifier);
        Assert.IsFalse(saved.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(saved.Contains("ApiAddress", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(saved.Contains("secret-value", StringComparison.Ordinal));
        Assert.IsFalse(saved.Contains("ModelName", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task LoadAsync_InvalidJson_RestoresDefaultsAndSetsLastLoadError()
    {
        using var temp = new TempDirectory();
        var settingsPath = temp.GetPath("settings.json");
        var defaults = CreateSettings(temp, "fallback");
        await File.WriteAllTextAsync(settingsPath, "{ invalid json");
        var service = new SettingsService(settingsPath, () => defaults);

        var loaded = await service.LoadAsync();

        Assert.AreEqual(defaults, loaded);
        Assert.IsInstanceOfType<JsonException>(service.LastLoadError);
    }

    [TestMethod]
    public async Task SaveAsync_ValidSettings_ReplacesFileAtomicallyWithoutTemporaryFiles()
    {
        using var temp = new TempDirectory();
        var settingsPath = temp.GetPath("settings.json");
        var service = new SettingsService(settingsPath, () => CreateSettings(temp, string.Empty));
        var settings = CreateSettings(temp, "fake/model");

        await service.SaveAsync(settings);

        var loaded = JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(settingsPath));
        Assert.AreEqual("fake/model", loaded?.SelectedModelSpecifier);
        Assert.AreEqual(0, Directory.GetFiles(temp.Path, "*.tmp", SearchOption.AllDirectories).Length);
    }

    [TestMethod]
    public async Task SaveAsync_FileOccupiesRequiredDirectory_ThrowsClearException()
    {
        using var temp = new TempDirectory();
        var blockedPath = temp.GetPath("blocked");
        await File.WriteAllTextAsync(blockedPath, "not a directory");
        var settings = CreateSettings(temp, string.Empty) with { CachePath = Path.Combine(blockedPath, "cache") };
        var service = new SettingsService(temp.GetPath("settings.json"), () => settings);

        await Assert.ThrowsAsync<IOException>(() => service.SaveAsync(settings));
    }

    private static AppSettings CreateSettings(TempDirectory temp, string selectedModel) => new()
    {
        CachePath = temp.GetPath("cache"),
        DataPath = temp.GetPath("data"),
        LogPath = temp.GetPath("logs"),
        ChatRequestTimeoutSeconds = 15,
        SendMessageWithEnter = true,
        SelectedModelSpecifier = selectedModel,
    };
}