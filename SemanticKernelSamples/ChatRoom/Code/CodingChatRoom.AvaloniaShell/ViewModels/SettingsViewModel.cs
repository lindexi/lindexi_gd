using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly CodingChatSettingsService? _settingsService;
    private readonly Action? _backAction;
    private readonly SimpleAsyncCommand _saveCommand;
    private string? _primaryModel;
    private bool _isWindowsSandboxEnabled = true;
    private string _windowsSandboxToolPath = "WinRemoteShell.exe";
    private string _windowsSandboxServerAddress = "127.0.0.1:12399";
    private string? _statusMessage;
    private bool _isStatusError;

    public SettingsViewModel()
    {
        _saveCommand = new SimpleAsyncCommand(static () => Task.CompletedTask, static () => false);
        BackCommand = new SimpleCommand(static () => { }, static () => false);
        AddProviderCommand = new SimpleCommand(AddProvider);
        RemoveProviderCommand = new SimpleCommand<ProviderSettingsViewModel>(RemoveProvider);
        Providers.Add(CreateEmptyProvider());
    }

    internal SettingsViewModel(CodingChatSettingsService settingsService, Action backAction)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(backAction);
        _settingsService = settingsService;
        _backAction = backAction;
        _saveCommand = new SimpleAsyncCommand(SaveAsync);
        BackCommand = new SimpleCommand(Back);
        AddProviderCommand = new SimpleCommand(AddProvider);
        RemoveProviderCommand = new SimpleCommand<ProviderSettingsViewModel>(RemoveProvider);
    }

    public ObservableCollection<ProviderSettingsViewModel> Providers { get; } = [];

    public string? PrimaryModel
    {
        get => _primaryModel;
        set => SetField(ref _primaryModel, value);
    }

    public bool IsWindowsSandboxEnabled
    {
        get => _isWindowsSandboxEnabled;
        set => SetField(ref _isWindowsSandboxEnabled, value);
    }

    public string WindowsSandboxToolPath
    {
        get => _windowsSandboxToolPath;
        set => SetField(ref _windowsSandboxToolPath, value);
    }

    public string WindowsSandboxServerAddress
    {
        get => _windowsSandboxServerAddress;
        set => SetField(ref _windowsSandboxServerAddress, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (SetField(ref _statusMessage, value))
            {
                OnPropertyChanged(nameof(HasStatusMessage));
            }
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetField(ref _isStatusError, value);
    }

    public ICommand SaveCommand => _saveCommand;

    public ICommand BackCommand { get; }

    public ICommand AddProviderCommand { get; }

    public ICommand RemoveProviderCommand { get; }

    internal async Task LoadAsync()
    {
        if (_settingsService is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            CodingChatSettingsSnapshot snapshot = await _settingsService.LoadAsync().ConfigureAwait(true);
            Load(snapshot);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Load(CodingChatSettingsSnapshot snapshot)
    {
        Providers.Clear();
        PrimaryModel = snapshot.ModelConfiguration?.PrimaryModel;

        if (snapshot.ModelConfiguration?.OpenAIConfigurationList is { } configurations)
        {
            foreach (OpenAIProtocolLanguageModelConfiguration configuration in configurations)
            {
                Providers.Add(ProviderSettingsViewModel.FromConfiguration(configuration));
            }
        }

        if (Providers.Count == 0)
        {
            Providers.Add(CreateEmptyProvider());
        }

        IsWindowsSandboxEnabled = snapshot.ShellSettings.IsWindowsSandboxEnabled;
        WindowsSandboxToolPath = snapshot.ShellSettings.WindowsSandboxToolPath;
        WindowsSandboxServerAddress = snapshot.ShellSettings.WindowsSandboxServerAddress;

        if (!string.IsNullOrWhiteSpace(snapshot.ModelConfigurationError))
        {
            SetStatus($"现有模型配置无法读取，请修正后保存：{snapshot.ModelConfigurationError}", isError: true);
        }
        else
        {
            StatusMessage = null;
        }
    }

    private async Task SaveAsync()
    {
        if (_settingsService is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            var modelConfiguration = new AgentApiManagerConfiguration
            {
                PrimaryModel = PrimaryModel,
                OpenAIConfigurationList = Providers.Select(provider => provider.ToConfiguration()).ToArray(),
            };
            var shellSettings = new CodingChatShellSettings
            {
                IsWindowsSandboxEnabled = IsWindowsSandboxEnabled,
                WindowsSandboxToolPath = WindowsSandboxToolPath,
                WindowsSandboxServerAddress = WindowsSandboxServerAddress,
            };

            await _settingsService.SaveAsync(modelConfiguration, shellSettings).ConfigureAwait(true);
            SetStatus("设置已保存。模型和沙箱配置将在下次启动时生效。", isError: false);
        }
        catch (ArgumentException exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        catch (JsonException exception)
        {
            SetStatus(exception.Message, isError: true);
        }
        catch (IOException exception)
        {
            SetStatus($"保存设置失败：{exception.Message}", isError: true);
        }
        catch (UnauthorizedAccessException exception)
        {
            SetStatus($"没有权限保存设置：{exception.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Back()
    {
        StatusMessage = null;
        _backAction?.Invoke();
    }

    private void AddProvider() => Providers.Add(CreateEmptyProvider());

    private void RemoveProvider(ProviderSettingsViewModel? provider)
    {
        if (provider is not null && Providers.Count > 1)
        {
            Providers.Remove(provider);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }

    private static ProviderSettingsViewModel CreateEmptyProvider()
    {
        var provider = new ProviderSettingsViewModel
        {
            EndPoint = "https://api.openai.com/v1",
        };
        provider.Models.Add(new ModelSettingsViewModel());
        return provider;
    }
}

public sealed class ProviderSettingsViewModel : ViewModelBase
{
    private string _endPoint = string.Empty;
    private string _apiKey = string.Empty;

    public ProviderSettingsViewModel()
    {
        AddModelCommand = new SimpleCommand(AddModel);
        RemoveModelCommand = new SimpleCommand<ModelSettingsViewModel>(RemoveModel);
    }

    public string EndPoint
    {
        get => _endPoint;
        set => SetField(ref _endPoint, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    public ObservableCollection<ModelSettingsViewModel> Models { get; } = [];

    public ICommand AddModelCommand { get; }

    public ICommand RemoveModelCommand { get; }

    internal OpenAIProtocolLanguageModelConfiguration ToConfiguration()
    {
        return new OpenAIProtocolLanguageModelConfiguration(EndPoint, ApiKey)
        {
            ModelDefinitions = Models.Select(model => model.ToDefinition()).ToArray(),
        };
    }

    internal static ProviderSettingsViewModel FromConfiguration(OpenAIProtocolLanguageModelConfiguration configuration)
    {
        var viewModel = new ProviderSettingsViewModel
        {
            EndPoint = configuration.EndPoint,
            ApiKey = configuration.Key,
        };

        if (configuration.ModelDefinitions is { } definitions)
        {
            foreach (ModelDefinition definition in definitions)
            {
                viewModel.Models.Add(ModelSettingsViewModel.FromDefinition(definition));
            }
        }

        if (viewModel.Models.Count == 0)
        {
            viewModel.Models.Add(new ModelSettingsViewModel());
        }

        return viewModel;
    }

    private void AddModel() => Models.Add(new ModelSettingsViewModel());

    private void RemoveModel(ModelSettingsViewModel? model)
    {
        if (model is not null && Models.Count > 1)
        {
            Models.Remove(model);
        }
    }
}

public sealed class ModelSettingsViewModel : ViewModelBase
{
    private string _provider = string.Empty;
    private string _modelName = string.Empty;
    private string? _modelId;
    private int? _contextWindowSize;
    private int? _maxOutputTokens;
    private bool _supportsReasoning;
    private bool _supportsToolCall = true;
    private bool _supportsImageInput;
    private bool _isFlash;

    public string Provider
    {
        get => _provider;
        set => SetField(ref _provider, value);
    }

    public string ModelName
    {
        get => _modelName;
        set => SetField(ref _modelName, value);
    }

    public string? ModelId
    {
        get => _modelId;
        set => SetField(ref _modelId, value);
    }

    public int? ContextWindowSize
    {
        get => _contextWindowSize;
        set => SetField(ref _contextWindowSize, value);
    }

    public int? MaxOutputTokens
    {
        get => _maxOutputTokens;
        set => SetField(ref _maxOutputTokens, value);
    }

    public bool SupportsReasoning
    {
        get => _supportsReasoning;
        set => SetField(ref _supportsReasoning, value);
    }

    public bool SupportsToolCall
    {
        get => _supportsToolCall;
        set => SetField(ref _supportsToolCall, value);
    }

    public bool SupportsImageInput
    {
        get => _supportsImageInput;
        set => SetField(ref _supportsImageInput, value);
    }

    public bool IsFlash
    {
        get => _isFlash;
        set => SetField(ref _isFlash, value);
    }

    internal ModelDefinition ToDefinition()
    {
        return new ModelDefinition
        {
            Provider = Provider,
            ModelName = ModelName,
            ModelId = ModelId,
            ContextWindowSize = ContextWindowSize,
            MaxOutputTokens = MaxOutputTokens,
            Capabilities = new LlmModelCapabilities
            {
                Reasoning = SupportsReasoning,
                ToolCall = SupportsToolCall,
                IsFlash = IsFlash,
                Attachment = SupportsImageInput,
                Input = new LlmModalityCapability { Image = SupportsImageInput },
            },
        };
    }

    internal static ModelSettingsViewModel FromDefinition(ModelDefinition definition)
    {
        return new ModelSettingsViewModel
        {
            Provider = definition.Provider,
            ModelName = definition.ModelName,
            ModelId = definition.ModelId,
            ContextWindowSize = definition.ContextWindowSize,
            MaxOutputTokens = definition.MaxOutputTokens,
            SupportsReasoning = definition.Capabilities?.Reasoning ?? false,
            SupportsToolCall = definition.Capabilities?.ToolCall ?? true,
            SupportsImageInput = definition.Capabilities?.Input.Image ?? false,
            IsFlash = definition.Capabilities?.IsFlash ?? false,
        };
    }
}
