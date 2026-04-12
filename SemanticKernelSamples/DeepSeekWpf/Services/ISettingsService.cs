using DeepSeekWpf.Models;

namespace DeepSeekWpf.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }

    void Save(AppSettings settings);

    void RestoreDefaults();
}
