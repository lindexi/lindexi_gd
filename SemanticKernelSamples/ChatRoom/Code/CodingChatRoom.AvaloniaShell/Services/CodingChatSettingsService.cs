using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using CodingChatRoom.AvaloniaShell.Infrastructure;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class CodingChatSettingsService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly CodingChatRoomPaths _paths;

    public CodingChatSettingsService(CodingChatRoomPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths = paths;
    }

    public async Task<CodingChatSettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureDirectories();

        CodingChatShellSettings shellSettings = await LoadShellSettingsAsync(cancellationToken).ConfigureAwait(false);
        AgentApiManagerConfiguration? modelConfiguration = null;
        string? modelConfigurationError = null;

        _paths.ConfigurationFile.Refresh();
        if (_paths.ConfigurationFile.Exists)
        {
            try
            {
                modelConfiguration = await AgentApiManagerConfiguration
                    .FromJsonFileAsync(_paths.ConfigurationFile)
                    .ConfigureAwait(false);
            }
            catch (JsonException exception)
            {
                modelConfigurationError = exception.Message;
            }
        }

        return new CodingChatSettingsSnapshot(modelConfiguration, shellSettings, modelConfigurationError);
    }

    public async Task SaveAsync(
        AgentApiManagerConfiguration modelConfiguration,
        CodingChatShellSettings shellSettings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modelConfiguration);
        ArgumentNullException.ThrowIfNull(shellSettings);
        cancellationToken.ThrowIfCancellationRequested();

        _paths.EnsureDirectories();

        await modelConfiguration.SaveToFileAsync(_paths.ConfigurationFile).ConfigureAwait(false);

        string shellJson = JsonSerializer.Serialize(shellSettings, s_jsonOptions);
        await File.WriteAllTextAsync(
            _paths.ShellSettingsFile.FullName,
            shellJson,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<CodingChatShellSettings> LoadShellSettingsAsync(CancellationToken cancellationToken = default)
    {
        _paths.ShellSettingsFile.Refresh();
        if (!_paths.ShellSettingsFile.Exists)
        {
            return new CodingChatShellSettings();
        }

        string json = await File.ReadAllTextAsync(
            _paths.ShellSettingsFile.FullName,
            cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<CodingChatShellSettings>(json, s_jsonOptions)
            ?? new CodingChatShellSettings();
    }

}

internal sealed record CodingChatSettingsSnapshot(
    AgentApiManagerConfiguration? ModelConfiguration,
    CodingChatShellSettings ShellSettings,
    string? ModelConfigurationError);
