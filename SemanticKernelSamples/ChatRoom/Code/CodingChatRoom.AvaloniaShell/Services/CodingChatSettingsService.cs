using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.Core;
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

        Validate(modelConfiguration, shellSettings);
        _paths.EnsureDirectories();

        var endpointManager = new AgentApiEndpointManager();
        endpointManager.LoadConfiguration(modelConfiguration);
        _ = endpointManager.PrimaryModel;

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

    private static void Validate(
        AgentApiManagerConfiguration modelConfiguration,
        CodingChatShellSettings shellSettings)
    {
        if (modelConfiguration.OpenAIConfigurationList is not { Count: > 0 })
        {
            throw new ArgumentException("请至少配置一个模型服务。", nameof(modelConfiguration));
        }

        if (string.IsNullOrWhiteSpace(modelConfiguration.PrimaryModel))
        {
            throw new ArgumentException("请选择首选模型。", nameof(modelConfiguration));
        }

        foreach (OpenAIProtocolLanguageModelConfiguration provider in modelConfiguration.OpenAIConfigurationList)
        {
            if (string.IsNullOrWhiteSpace(provider.EndPoint))
            {
                throw new ArgumentException("模型服务地址不能为空。", nameof(modelConfiguration));
            }

            if (string.IsNullOrWhiteSpace(provider.Key))
            {
                throw new ArgumentException("API 密钥不能为空。", nameof(modelConfiguration));
            }

            if (provider.ModelDefinitions is not { Count: > 0 })
            {
                throw new ArgumentException("每个模型服务至少需要一个模型。", nameof(modelConfiguration));
            }
        }

        if (!shellSettings.IsWindowsSandboxEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(shellSettings.WindowsSandboxToolPath))
        {
            throw new ArgumentException("沙箱工具路径不能为空。", nameof(shellSettings));
        }

        if (string.IsNullOrWhiteSpace(shellSettings.WindowsSandboxServerAddress))
        {
            throw new ArgumentException("沙箱连接地址不能为空。", nameof(shellSettings));
        }
    }
}

internal sealed record CodingChatSettingsSnapshot(
    AgentApiManagerConfiguration? ModelConfiguration,
    CodingChatShellSettings ShellSettings,
    string? ModelConfigurationError);
