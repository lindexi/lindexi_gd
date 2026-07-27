using System.Collections.ObjectModel;
using DeepSeekWpf.Infrastructure;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;

namespace DeepSeekWpf.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ChatWorkspaceViewModel _chatWorkspaceViewModel;
    private readonly IAgentModelService _agentModelService;
    private readonly IModelConnectionTestService _connectionTestService;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly IUserInteractionService _userInteractionService;
    private readonly IAppLogger _logger;
    private readonly AsyncRelayCommand _saveSettingsCommand;
    private readonly AsyncRelayCommand _restoreDefaultsCommand;
    private readonly AsyncRelayCommand _reloadAgentConfigurationCommand;
    private readonly AsyncRelayCommand _testConnectionCommand;
    private readonly AsyncRelayCommand _copyDiagnosticsCommand;
    private readonly AsyncRelayCommand _clearLogsCommand;
    private readonly AsyncRelayCommand _openAgentConfigurationCommand;
    private readonly AsyncRelayCommand _openLogDirectoryCommand;
    private readonly AsyncRelayCommand _openFolderCommand;
    private string _cachePath = string.Empty;
    private string _dataPath = string.Empty;
    private string _logPath = string.Empty;
    private AgentModelDescriptor? _selectedModel;
    private string _statusMessage = "就绪";
    private int _chatRequestTimeoutSeconds = 120;
    private bool _isBusy;

    public SettingsViewModel(
        ISettingsService settingsService,
        ChatWorkspaceViewModel chatWorkspaceViewModel,
        IAgentModelService agentModelService,
        IModelConnectionTestService connectionTestService,
        IDiagnosticsService diagnosticsService,
        IUserInteractionService userInteractionService,
        IAppLogger logger)
    {
        _settingsService = settingsService;
        _chatWorkspaceViewModel = chatWorkspaceViewModel;
        _agentModelService = agentModelService;
        _connectionTestService = connectionTestService;
        _diagnosticsService = diagnosticsService;
        _userInteractionService = userInteractionService;
        _logger = logger;
        RegisteredModels = [];
        _saveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync, CanSaveSettings);
        _restoreDefaultsCommand = new AsyncRelayCommand(RestoreDefaultsAsync);
        _reloadAgentConfigurationCommand = new AsyncRelayCommand(ReloadAgentConfigurationAsync);
        _testConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, CanTestConnection);
        _copyDiagnosticsCommand = new AsyncRelayCommand(CopyDiagnosticsAsync, () => !IsBusy);
        _clearLogsCommand = new AsyncRelayCommand(ClearLogsAsync, () => !IsBusy);
        _openAgentConfigurationCommand = new AsyncRelayCommand(OpenAgentConfigurationAsync, () => !IsBusy);
        _openLogDirectoryCommand = new AsyncRelayCommand(OpenLogDirectoryAsync, () => !IsBusy);
        _openFolderCommand = new AsyncRelayCommand(OpenFolderAsync, CanOpenFolder);

        ReloadFromServices();
    }

    public int ChatRequestTimeoutSeconds
    {
        get => _chatRequestTimeoutSeconds;
        set
        {
            if (SetProperty(ref _chatRequestTimeoutSeconds, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public event EventHandler? SaveCompleted;

    public string CachePath
    {
        get => _cachePath;
        set
        {
            if (SetProperty(ref _cachePath, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
                _openFolderCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string DataPath
    {
        get => _dataPath;
        set
        {
            if (SetProperty(ref _dataPath, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
                _openFolderCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string LogPath
    {
        get => _logPath;
        set
        {
            if (SetProperty(ref _logPath, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
                _openFolderCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<AgentModelDescriptor> RegisteredModels { get; }

    public AgentModelDescriptor? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (SetProperty(ref _selectedModel, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
                _testConnectionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommandStates();
            }
        }
    }

    public string AgentConfigurationFilePath => _agentModelService.ConfigurationFilePath;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public AsyncRelayCommand SaveSettingsCommand => _saveSettingsCommand;

    public AsyncRelayCommand RestoreDefaultsCommand => _restoreDefaultsCommand;

    public AsyncRelayCommand ReloadAgentConfigurationCommand => _reloadAgentConfigurationCommand;

    public AsyncRelayCommand TestConnectionCommand => _testConnectionCommand;

    public AsyncRelayCommand CopyDiagnosticsCommand => _copyDiagnosticsCommand;

    public AsyncRelayCommand ClearLogsCommand => _clearLogsCommand;

    public AsyncRelayCommand OpenAgentConfigurationCommand => _openAgentConfigurationCommand;

    public AsyncRelayCommand OpenLogDirectoryCommand => _openLogDirectoryCommand;

    public AsyncRelayCommand OpenFolderCommand => _openFolderCommand;

    public void ReloadFromServices()
    {
        var settings = _settingsService.CurrentSettings;
        CachePath = settings.CachePath;
        DataPath = settings.DataPath;
        LogPath = settings.LogPath;
        ChatRequestTimeoutSeconds = settings.ChatRequestTimeoutSeconds;
        RefreshRegisteredModels(settings.SelectedModelSpecifier);
        StatusMessage = "已加载当前设置";
    }

    private async Task SaveSettingsAsync()
    {
        var selectedSpecifier = SelectedModel?.Specifier ?? string.Empty;
        var settings = _settingsService.CurrentSettings with
        {
            CachePath = CachePath.Trim(),
            DataPath = DataPath.Trim(),
            LogPath = LogPath.Trim(),
            ChatRequestTimeoutSeconds = ChatRequestTimeoutSeconds,
            SelectedModelSpecifier = selectedSpecifier,
        };

        try
        {
            if (!string.IsNullOrWhiteSpace(selectedSpecifier))
            {
                _agentModelService.SelectModel(selectedSpecifier);
                _chatWorkspaceViewModel.RefreshConfigurationState();
            }

            await _settingsService.SaveAsync(settings);
            await _chatWorkspaceViewModel.ReloadSessionsAsync();
            StatusMessage = "设置已保存";
            await _logger.InformationAsync("保存产品设置");
            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            StatusMessage = $"保存设置失败：{exception.Message}";
            await _logger.ErrorAsync("保存产品设置失败", exception);
        }
    }

    private bool CanSaveSettings()
    {
        return !string.IsNullOrWhiteSpace(CachePath) &&
               !string.IsNullOrWhiteSpace(DataPath) &&
               !string.IsNullOrWhiteSpace(LogPath) &&
               ChatRequestTimeoutSeconds is >= 10 and <= 3600;
    }

    private async Task RestoreDefaultsAsync()
    {
        try
        {
            await _settingsService.RestoreDefaultsAsync();
            ReloadFromServices();
            await _chatWorkspaceViewModel.ReloadSessionsAsync();
            StatusMessage = "已恢复默认设置";
            await _logger.InformationAsync("恢复默认产品设置");
        }
        catch (Exception exception)
        {
            StatusMessage = $"恢复默认设置失败：{exception.Message}";
            await _logger.ErrorAsync("恢复默认产品设置失败", exception);
        }
    }

    private async Task ReloadAgentConfigurationAsync()
    {
        try
        {
            StatusMessage = "正在重新加载 Agent 配置...";
            await _agentModelService.ReloadAsync();
            RefreshRegisteredModels(_settingsService.CurrentSettings.SelectedModelSpecifier);
            _chatWorkspaceViewModel.RefreshConfigurationState();
            StatusMessage = $"已加载 {RegisteredModels.Count} 个模型";
            await _logger.InformationAsync("重新加载 Agent 模型配置");
        }
        catch (Exception exception)
        {
            StatusMessage = $"重新加载 Agent 配置失败：{exception.Message}";
            await _logger.ErrorAsync("重新加载 Agent 模型配置失败", exception);
        }
    }

    private async Task TestConnectionAsync()
    {
        if (SelectedModel is null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "正在测试模型连接...";
            _agentModelService.SelectModel(SelectedModel.Specifier);
            _chatWorkspaceViewModel.RefreshConfigurationState();
            var result = await _connectionTestService.TestAsync();
            StatusMessage = result.Message;
            if (result.IsSuccess)
            {
                await _logger.InformationAsync($"模型连接测试成功，模型：{SelectedModel.Specifier}");
            }
            else
            {
                await _logger.WarningAsync($"模型连接测试失败，模型：{SelectedModel.Specifier}，类别：{result.ErrorCategory}");
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "已取消模型连接测试";
        }
        catch (Exception exception)
        {
            StatusMessage = "模型连接测试失败，请查看日志";
            await _logger.ErrorAsync("模型连接测试发生异常", exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanTestConnection() => SelectedModel is not null && !IsBusy;

    private async Task CopyDiagnosticsAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            var summary = await _diagnosticsService.CreateSummaryAsync();
            await _userInteractionService.CopyTextAsync(summary);
            StatusMessage = "诊断摘要已复制";
            await _logger.InformationAsync("复制诊断摘要");
        }, "复制诊断摘要失败");
    }

    private async Task ClearLogsAsync()
    {
        if (!await _userInteractionService.ConfirmAsync("清理日志", "确定要清理应用日志吗？"))
        {
            return;
        }

        await ExecuteBusyActionAsync(async () =>
        {
            await _diagnosticsService.ClearLogsAsync();
            StatusMessage = "日志已清理";
        }, "清理日志失败");
    }

    private Task OpenAgentConfigurationAsync() => OpenPathAsync(AgentConfigurationFilePath, "打开 Agent 配置文件失败");

    private Task OpenLogDirectoryAsync() => OpenPathAsync(LogPath, "打开日志目录失败");

    private async Task OpenFolderAsync(object? parameter)
    {
        if (parameter is string path)
        {
            await OpenPathAsync(path.Trim(), "打开目录失败");
        }
    }

    private Task OpenPathAsync(string path, string failureMessage)
    {
        return ExecuteBusyActionAsync(async () =>
        {
            await _userInteractionService.OpenPathAsync(path);
            StatusMessage = "已打开指定位置";
            await _logger.InformationAsync("打开应用配置或数据位置");
        }, failureMessage);
    }

    private async Task ExecuteBusyActionAsync(Func<Task> action, string failureMessage)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "操作已取消";
        }
        catch (Exception exception)
        {
            StatusMessage = failureMessage;
            await _logger.ErrorAsync(failureMessage, exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanOpenFolder(object? parameter)
    {
        return parameter is string path && !string.IsNullOrWhiteSpace(path) && !IsBusy;
    }

    private void RefreshRegisteredModels(string selectedSpecifier)
    {
        RegisteredModels.Clear();
        foreach (var model in _agentModelService.RegisteredModels)
        {
            RegisteredModels.Add(model);
        }

        SelectedModel = RegisteredModels.FirstOrDefault(model =>
            string.Equals(model.Specifier, selectedSpecifier, StringComparison.OrdinalIgnoreCase))
            ?? _agentModelService.SelectedModel;
    }

    private void NotifyCommandStates()
    {
        _saveSettingsCommand.NotifyCanExecuteChanged();
        _restoreDefaultsCommand.NotifyCanExecuteChanged();
        _reloadAgentConfigurationCommand.NotifyCanExecuteChanged();
        _testConnectionCommand.NotifyCanExecuteChanged();
        _copyDiagnosticsCommand.NotifyCanExecuteChanged();
        _clearLogsCommand.NotifyCanExecuteChanged();
        _openAgentConfigurationCommand.NotifyCanExecuteChanged();
        _openLogDirectoryCommand.NotifyCanExecuteChanged();
        _openFolderCommand.NotifyCanExecuteChanged();
    }
}
