using System.IO;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using DeepSeekWpf.Models;
using Microsoft.Extensions.AI;

namespace DeepSeekWpf.Services;

public sealed class AgentModelService : IAgentModelService
{
    public const string ConfigurationPathEnvironmentVariable = "DEEPSEEKWPF_AGENT_CONFIG";
    private const string EmptyConfigurationFileContent =
        """
        {
          "PrimaryModel": "",
          "OpenAIConfigurationList": []
        }
        """;

    private readonly IAgentApiEndpointManagerFactory _managerFactory;
    private readonly IAppLogger _logger;
    private readonly Lock _stateLock = new();
    private readonly SemaphoreSlim _reloadLock = new(1, 1);
    private AgentApiEndpointManager? _manager;
    private IReadOnlyList<AgentModelDescriptor> _registeredModels = [];
    private AgentModelDescriptor? _selectedModel;

    public AgentModelService(
        IAgentApiEndpointManagerFactory managerFactory,
        IAppLogger logger,
        string? configurationFilePath = null)
    {
        _managerFactory = managerFactory;
        _logger = logger;
        ConfigurationFilePath = Path.GetFullPath(
            configurationFilePath ?? ResolveConfigurationFilePath());
    }

    public string ConfigurationFilePath { get; }

    public IReadOnlyList<AgentModelDescriptor> RegisteredModels
    {
        get
        {
            lock (_stateLock)
            {
                return _registeredModels;
            }
        }
    }

    public AgentModelDescriptor? SelectedModel
    {
        get
        {
            lock (_stateLock)
            {
                return _selectedModel;
            }
        }
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureConfigurationFileAsync(cancellationToken).ConfigureAwait(false);

            var configuration = await AgentApiManagerConfiguration
                .FromJsonFileAsync(new FileInfo(ConfigurationFilePath))
                .ConfigureAwait(false);

            var manager = _managerFactory.Create();
            manager.LoadConfiguration(configuration);

            var languageModels = manager.GetSupportedModels();
            var registeredModels = languageModels.Select(CreateDescriptor).ToArray();
            var selectedModel = languageModels.Count == 0
                ? null
                : CreateDescriptor(manager.PrimaryModel);

            lock (_stateLock)
            {
                _manager = manager;
                _registeredModels = registeredModels;
                _selectedModel = selectedModel;
            }

            await _logger.InformationAsync(
                $"Agent 模型配置已加载，注册模型数：{registeredModels.Length}",
                cancellationToken);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync("加载 Agent 模型配置失败", exception, cancellationToken);
            throw;
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void SelectModel(string modelSpecifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelSpecifier);

        lock (_stateLock)
        {
            var manager = _manager ?? throw new InvalidOperationException("Agent 配置尚未加载。");
            var model = manager.ResolveModel(modelSpecifier)
                ?? throw new ArgumentException($"未找到模型 '{modelSpecifier}'。", nameof(modelSpecifier));

            manager.PrimaryModel = model;
            _selectedModel = CreateDescriptor(model);
        }
    }

    public Task<IChatClient> GetSelectedChatClientAsync()
    {
        lock (_stateLock)
        {
            var manager = _manager ?? throw new InvalidOperationException("Agent 配置尚未加载。");
            if (manager.GetSupportedModels().Count == 0)
            {
                throw new InvalidOperationException("Agent 配置中没有可用模型。");
            }

            return manager.PrimaryModel.GetChatClientAsync();
        }
    }

    public static string ResolveConfigurationFilePath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(ConfigurationPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekWpf",
            "AgentConfiguration.json");
    }

    private async Task EnsureConfigurationFileAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(ConfigurationFilePath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(ConfigurationFilePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(
            ConfigurationFilePath,
            EmptyConfigurationFileContent,
            cancellationToken).ConfigureAwait(false);
    }

    private static AgentModelDescriptor CreateDescriptor(ILanguageModel languageModel)
    {
        var definition = languageModel.ModelDefinition;
        return new AgentModelDescriptor(
            $"{definition.Provider}/{definition.ModelName}",
            definition.Provider,
            definition.ModelName,
            definition.ModelId,
            definition.Capabilities,
            definition.ContextWindowSize,
            definition.MaxOutputTokens);
    }
}