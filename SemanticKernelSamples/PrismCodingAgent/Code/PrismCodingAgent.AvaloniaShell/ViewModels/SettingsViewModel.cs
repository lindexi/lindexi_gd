using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using PrismCodingAgent.AvaloniaShell.Services.Integrations.WindowsSandbox;
using PrismCodingAgent.AvaloniaShell.Services.Settings;

namespace PrismCodingAgent.AvaloniaShell.ViewModels;

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly CodingChatSettingsService? _settingsService;
    private readonly IWindowsSandboxConnectionTester? _windowsSandboxConnectionTester;
    private readonly Action? _backAction;
    private readonly SimpleAsyncCommand _saveCommand;
    private readonly SimpleAsyncCommand _testWindowsSandboxConnectionCommand;
    private CancellationTokenSource? _sandboxConnectionTestCancellationTokenSource;
    private string? _primaryModel;
    private string _languageTag = Localizations.Lang.Current.IetfLanguageTag;

    /// <summary>获取可选择的界面语言标签。</summary>
    public string[] LanguageTags { get; } = ["zh-Hans", "en"];

    /// <summary>获取或设置保存后应用的界面语言。</summary>
    public string LanguageTag
    {
        get => _languageTag;
        set => SetField(ref _languageTag, value == "en" ? "en" : "zh-Hans");
    }
    private bool _isWindowsSandboxEnabled = true;
    private string _windowsSandboxToolPath = "WinRemoteShell.exe";
    private string _windowsSandboxServerAddress = "127.0.0.1:12399";
    private string? _sandboxConnectionStatusMessage;
    private bool _isSandboxConnectionStatusError;
    private bool _isSandboxConnectionTestRunning;
    private string? _deepSeekApiKey;
    private bool _hasExplicitDeepSeekApiKey;
    private bool _isLoading;
    private bool _isDisposed;
    private readonly HashSet<ProviderSettingsViewModel> _subscribedProviders = [];
    private bool _isNetworkProxyEnabled;
    private string? _networkProxyAddress;
    private bool _bypassProxyOnLocal = true;
    private bool _isCopilotInstructionsEnabled;
    private string? _copilotInstructionsPath;
    private string? _statusMessage;
    private bool _isStatusError;

    public SettingsViewModel()
    {
        _saveCommand = new SimpleAsyncCommand(static () => Task.CompletedTask, static () => false);
        _testWindowsSandboxConnectionCommand = new SimpleAsyncCommand
            (static () => Task.CompletedTask, static () => false);
        BackCommand = new SimpleCommand(static () => { }, static () => false);
        AddProviderCommand = new SimpleCommand(AddProvider);
        RemoveProviderCommand = new SimpleCommand<ProviderSettingsViewModel>(RemoveProvider);
        Providers.CollectionChanged += OnProvidersChanged;
        Providers.Add(CreateEmptyProvider());
    }

    internal SettingsViewModel(CodingChatSettingsService settingsService, Action backAction)
        : this(settingsService, new WindowsSandboxConnectionTester(), backAction)
    {
    }

    internal SettingsViewModel
    (
        CodingChatSettingsService settingsService,
        IWindowsSandboxConnectionTester windowsSandboxConnectionTester,
        Action backAction
    )
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(windowsSandboxConnectionTester);
        ArgumentNullException.ThrowIfNull(backAction);
        _settingsService = settingsService;
        _windowsSandboxConnectionTester = windowsSandboxConnectionTester;
        _backAction = backAction;
        _saveCommand = new SimpleAsyncCommand(SaveAsync, exceptionHandler: HandleCommandException);
        _testWindowsSandboxConnectionCommand = new SimpleAsyncCommand
            (TestWindowsSandboxConnectionAsync, CanTestWindowsSandboxConnection, exceptionHandler: HandleCommandException);
        BackCommand = new SimpleCommand(Back);
        AddProviderCommand = new SimpleCommand(AddProvider);
        RemoveProviderCommand = new SimpleCommand<ProviderSettingsViewModel>(RemoveProvider);
        Providers.CollectionChanged += OnProvidersChanged;
        Providers.Add(CreateEmptyProvider());
    }

    public ObservableCollection<ProviderSettingsViewModel> Providers { get; } = [];

    internal event EventHandler<CodingChatSettingsSavedEventArgs>? SettingsSaved;

    public string? PrimaryModel
    {
        get => _primaryModel;
        set => SetField(ref _primaryModel, value);
    }

    public bool IsWindowsSandboxEnabled
    {
        get => _isWindowsSandboxEnabled;
        set
        {
            if (SetField(ref _isWindowsSandboxEnabled, value))
            {
                _testWindowsSandboxConnectionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string WindowsSandboxToolPath
    {
        get => _windowsSandboxToolPath;
        set
        {
            if (SetField(ref _windowsSandboxToolPath, value))
            {
                _testWindowsSandboxConnectionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string WindowsSandboxServerAddress
    {
        get => _windowsSandboxServerAddress;
        set
        {
            value = value.Trim();
            if (SetField(ref _windowsSandboxServerAddress, value))
            {
                _testWindowsSandboxConnectionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>获取或设置 DeepSeek 官方网络工具使用的 API 密钥。</summary>
    public string? DeepSeekApiKey
    {
        get => _deepSeekApiKey;
        set
        {
            _hasExplicitDeepSeekApiKey = !string.IsNullOrWhiteSpace(value);
            SetField(ref _deepSeekApiKey, value);
            RefreshDeepSeekApiKey();
        }
    }

    public bool IsNetworkProxyEnabled
    {
        get => _isNetworkProxyEnabled;
        set => SetField(ref _isNetworkProxyEnabled, value);
    }

    public string? NetworkProxyAddress
    {
        get => _networkProxyAddress;
        set => SetField(ref _networkProxyAddress, value);
    }

    public bool BypassProxyOnLocal
    {
        get => _bypassProxyOnLocal;
        set => SetField(ref _bypassProxyOnLocal, value);
    }

    public string? SandboxConnectionStatusMessage
    {
        get => _sandboxConnectionStatusMessage;
        private set
        {
            if (SetField(ref _sandboxConnectionStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasSandboxConnectionStatusMessage));
            }
        }
    }

    public bool HasSandboxConnectionStatusMessage => !string.IsNullOrWhiteSpace(SandboxConnectionStatusMessage);

    public bool IsSandboxConnectionStatusError
    {
        get => _isSandboxConnectionStatusError;
        private set => SetField(ref _isSandboxConnectionStatusError, value);
    }

    public bool IsSandboxConnectionTestRunning
    {
        get => _isSandboxConnectionTestRunning;
        private set
        {
            if (SetField(ref _isSandboxConnectionTestRunning, value))
            {
                _testWindowsSandboxConnectionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsCopilotInstructionsEnabled
    {
        get => _isCopilotInstructionsEnabled;
        set => SetField(ref _isCopilotInstructionsEnabled, value);
    }

    public string? CopilotInstructionsPath
    {
        get => _copilotInstructionsPath;
        set => SetField(ref _copilotInstructionsPath, value);
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

    public ICommand TestWindowsSandboxConnectionCommand => _testWindowsSandboxConnectionCommand;

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
        _isLoading = true;
        DeepSeekApiKey = snapshot.ShellSettings.DeepSeekApiKey;
        LanguageTag = snapshot.ShellSettings.LanguageTag;
        IsWindowsSandboxEnabled = snapshot.ShellSettings.IsWindowsSandboxEnabled;
        WindowsSandboxToolPath = snapshot.ShellSettings.WindowsSandboxToolPath;
        WindowsSandboxServerAddress = snapshot.ShellSettings.WindowsSandboxServerAddress;
        IsNetworkProxyEnabled = snapshot.ShellSettings.IsNetworkProxyEnabled;
        NetworkProxyAddress = snapshot.ShellSettings.NetworkProxyAddress;
        BypassProxyOnLocal = snapshot.ShellSettings.BypassProxyOnLocal;
        IsCopilotInstructionsEnabled = snapshot.ShellSettings.IsCopilotInstructionsEnabled;
        CopilotInstructionsPath = snapshot.ShellSettings.CopilotInstructionsPath;

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

        _isLoading = false;
        RefreshDeepSeekApiKey();

        if (!string.IsNullOrWhiteSpace(snapshot.ModelConfigurationError))
        {
            SetStatus(Localizations.Lang.Format(Localizations.Lang.Current.ModelReadFailed, snapshot.ModelConfigurationError), isError: true);
        }
        else
        {
            StatusMessage = null;
        }
    }

    private bool CanTestWindowsSandboxConnection()
        => IsWindowsSandboxEnabled
           && !IsSandboxConnectionTestRunning
           && !string.IsNullOrWhiteSpace(WindowsSandboxToolPath)
           && !string.IsNullOrWhiteSpace(WindowsSandboxServerAddress);

    private async Task TestWindowsSandboxConnectionAsync()
    {
        if (_windowsSandboxConnectionTester is null || !CanTestWindowsSandboxConnection())
        {
            return;
        }

        _sandboxConnectionTestCancellationTokenSource?.Cancel();
        _sandboxConnectionTestCancellationTokenSource?.Dispose();
        var cancellationTokenSource = new CancellationTokenSource();
        _sandboxConnectionTestCancellationTokenSource = cancellationTokenSource;
        IsSandboxConnectionTestRunning = true;
        IsSandboxConnectionStatusError = false;
        SandboxConnectionStatusMessage = Localizations.Lang.Current.SandboxConnecting;

        try
        {
            WindowsSandboxConnectionTestResult result = await _windowsSandboxConnectionTester.TestAsync
            (
                WindowsSandboxToolPath,
                WindowsSandboxServerAddress,
                cancellationTokenSource.Token
            ).ConfigureAwait(true);
            if (_sandboxConnectionTestCancellationTokenSource == cancellationTokenSource)
            {
                IsSandboxConnectionStatusError = !result.IsSuccessful;
                SandboxConnectionStatusMessage = result.Message;
            }
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
        }
        finally
        {
            if (_sandboxConnectionTestCancellationTokenSource == cancellationTokenSource)
            {
                _sandboxConnectionTestCancellationTokenSource = null;
                IsSandboxConnectionTestRunning = false;
            }

            cancellationTokenSource.Dispose();
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
            string? networkProxyAddress = ValidateNetworkProxyAddress();
            var configurations = Providers.Where(provider => provider.Models.Count > 0)
                .Select(provider => provider.ToConfiguration()).ToArray();
            configurations = configurations.Where(configuration => configuration.ModelDefinitions?.Count > 0).ToArray();
            var definitions = configurations.SelectMany(configuration => configuration.ModelDefinitions ?? []).ToArray();
            string? primaryModel = definitions.Any(model => $"{model.Provider}/{model.ModelName}" == PrimaryModel || model.ModelName == PrimaryModel)
                ? PrimaryModel : definitions.FirstOrDefault() is { } first ? $"{first.Provider}/{first.ModelName}" : null;
            var modelConfiguration = new AgentApiManagerConfiguration
            {
                PrimaryModel = primaryModel,
                OpenAIConfigurationList = configurations,
            };
            var shellSettings = new CodingChatShellSettings
            {
                DeepSeekApiKey = DeepSeekApiKeyResolver.Resolve(DeepSeekApiKey, modelConfiguration),
                LanguageTag = LanguageTag,
                IsWindowsSandboxEnabled = IsWindowsSandboxEnabled,
                WindowsSandboxToolPath = WindowsSandboxToolPath,
                WindowsSandboxServerAddress = WindowsSandboxServerAddress,
                IsNetworkProxyEnabled = IsNetworkProxyEnabled,
                NetworkProxyAddress = networkProxyAddress,
                BypassProxyOnLocal = BypassProxyOnLocal,
                IsCopilotInstructionsEnabled = IsCopilotInstructionsEnabled,
                CopilotInstructionsPath = string.IsNullOrWhiteSpace(CopilotInstructionsPath)
                    ? null
                    : CopilotInstructionsPath.Trim(),
            };

            var validationManager = new AgentLib.Core.AgentApiEndpointManager();
            try
            {
                validationManager.LoadConfiguration(modelConfiguration);
            }
            finally
            {
                validationManager.HttpClient?.Dispose();
            }
            await _settingsService.SaveAsync(modelConfiguration, shellSettings).ConfigureAwait(true);
            SetField(ref _deepSeekApiKey, shellSettings.DeepSeekApiKey, nameof(DeepSeekApiKey));
            Localizations.Lang.SetCurrent(LanguageTag);
            var settings = new CodingChatSettingsSnapshot(modelConfiguration, shellSettings, null);
            SettingsSaved?.Invoke(this, new CodingChatSettingsSavedEventArgs(settings));
            SetStatus(Localizations.Lang.Current.SettingsSaved, isError: false);
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
            SetStatus(Localizations.Lang.Format(Localizations.Lang.Current.SettingsSaveFailed, exception.Message), isError: true);
        }
        catch (UnauthorizedAccessException exception)
        {
            SetStatus(Localizations.Lang.Format(Localizations.Lang.Current.SettingsSaveDenied, exception.Message), isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string? ValidateNetworkProxyAddress()
    {
        if (!IsNetworkProxyEnabled)
        {
            return string.IsNullOrWhiteSpace(NetworkProxyAddress) ? null : NetworkProxyAddress.Trim();
        }

        if (string.IsNullOrWhiteSpace(NetworkProxyAddress))
        {
            throw new ArgumentException(Localizations.Lang.Current.ProxyRequired);
        }

        string address = NetworkProxyAddress.Trim();
        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? proxyUri)
            || (proxyUri.Scheme != Uri.UriSchemeHttp && proxyUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(Localizations.Lang.Current.ProxyInvalid);
        }

        return address;
    }

    private void Back()
    {
        StatusMessage = null;
        _backAction?.Invoke();
    }

    private void OnProvidersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var provider in _subscribedProviders.Where(provider => !Providers.Contains(provider)).ToArray())
        {
            provider.PropertyChanged -= OnProviderPropertyChanged;
            _subscribedProviders.Remove(provider);
        }

        foreach (var provider in Providers)
        {
            if (_subscribedProviders.Add(provider))
            {
                provider.PropertyChanged += OnProviderPropertyChanged;
            }
        }

        RefreshDeepSeekApiKey();
    }

    private void OnProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName is nameof(ProviderSettingsViewModel.ApiKey) or nameof(ProviderSettingsViewModel.EndPoint))
        {
            RefreshDeepSeekApiKey();
        }
    }

    private void RefreshDeepSeekApiKey()
    {
        if (_isDisposed || _isLoading || _hasExplicitDeepSeekApiKey) return;

        var configuration = new AgentApiManagerConfiguration
        {
            OpenAIConfigurationList = Providers.Select(provider =>
                new OpenAIProtocolLanguageModelConfiguration(provider.EndPoint, provider.ApiKey)).ToArray(),
        };
        SetField(ref _deepSeekApiKey, DeepSeekApiKeyResolver.Resolve(null, configuration), nameof(DeepSeekApiKey));
    }

    /// <summary>解除模型服务监听并取消尚未完成的沙箱连接检查。</summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Providers.CollectionChanged -= OnProvidersChanged;
        foreach (var provider in _subscribedProviders)
        {
            provider.PropertyChanged -= OnProviderPropertyChanged;
        }
        _subscribedProviders.Clear();
        _sandboxConnectionTestCancellationTokenSource?.Cancel();
    }

    private void AddProvider() => Providers.Add(CreateEmptyProvider());

    private void RemoveProvider(ProviderSettingsViewModel? provider)
    {
        if (provider is not null)
        {
            Providers.Remove(provider);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }

    private void HandleCommandException(Exception exception)
    {
        System.Diagnostics.Trace.TraceError($"设置操作失败：{exception}");
        SetStatus(Localizations.Lang.Format(Localizations.Lang.Current.SettingsOperationFailed, exception.Message), isError: true);
    }

    private static ProviderSettingsViewModel CreateEmptyProvider()
    {
        var provider = new ProviderSettingsViewModel
        {
            EndPoint = "https://api.openai.com/v1",
        };
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
            ModelDefinitions = Models.Where(model => !string.IsNullOrWhiteSpace(model.ModelName))
                .Select(model => model.ToDefinition()).ToArray(),
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

        return viewModel;
    }

    private void AddModel() => Models.Add(new ModelSettingsViewModel());

    private void RemoveModel(ModelSettingsViewModel? model)
    {
        if (model is not null)
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