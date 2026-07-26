using System.Text.Json;
using System.IO;
using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _settingsFilePath;
    private readonly Func<AppSettings> _defaultSettingsFactory;

    public SettingsService(
        string? settingsFilePath = null,
        Func<AppSettings>? defaultSettingsFactory = null)
    {
        _settingsFilePath = Path.GetFullPath(settingsFilePath ?? GetDefaultSettingsFilePath());
        _defaultSettingsFactory = defaultSettingsFactory ?? AppSettings.CreateDefault;
        CurrentSettings = _defaultSettingsFactory();
    }

    public AppSettings CurrentSettings { get; private set; }

    public string SettingsFilePath => _settingsFilePath;

    public Exception? LastLoadError { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await LoadAsync(cancellationToken).ConfigureAwait(false);
        await SaveAsync(settings, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LastLoadError = null;
            if (!File.Exists(_settingsFilePath))
            {
                CurrentSettings = _defaultSettingsFactory();
                return CurrentSettings;
            }

            try
            {
                await using var stream = new FileStream(
                    _settingsFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                    stream,
                    _serializerOptions,
                    cancellationToken).ConfigureAwait(false);
                CurrentSettings = settings ?? throw new JsonException("设置文件内容为空。 ");
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                LastLoadError = exception;
                CurrentSettings = _defaultSettingsFactory();
            }

            return CurrentSettings;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ValidateDirectoriesAsync(settings, cancellationToken).ConfigureAwait(false);
            await WriteAtomicallyAsync(settings, cancellationToken).ConfigureAwait(false);
            CurrentSettings = settings with { };
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public Task RestoreDefaultsAsync(CancellationToken cancellationToken = default)
    {
        return SaveAsync(_defaultSettingsFactory(), cancellationToken);
    }

    private static string GetDefaultSettingsFilePath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekWpf");
        return Path.Combine(appDataPath, "settings.json");
    }

    private async Task WriteAtomicallyAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_settingsFilePath)
            ?? throw new InvalidOperationException("无法确定设置目录。");
        Directory.CreateDirectory(directoryPath);

        var temporaryFilePath = Path.Combine(
            directoryPath,
            $".{Path.GetFileName(_settingsFilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                             temporaryFilePath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    _serializerOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryFilePath, _settingsFilePath, true);
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }

    private static async Task ValidateDirectoriesAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        foreach (var path in new[] { settings.CachePath, settings.DataPath, settings.LogPath })
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("应用目录不能为空。");
            }

            Directory.CreateDirectory(path);
            var probePath = Path.Combine(path, $".write-test-{Guid.NewGuid():N}.tmp");
            try
            {
                await File.WriteAllTextAsync(probePath, string.Empty, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
        }
    }
}
