using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }

    string SettingsFilePath { get; }

    Exception? LastLoadError { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);

    Task RestoreDefaultsAsync(CancellationToken cancellationToken = default);
}
