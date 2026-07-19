using System.Diagnostics;
using System.IO;
using DeepSeekWpf.Infrastructure;
using DeepSeekWpf.Services;

namespace DeepSeekWpf.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IAgentConfigurationService _agentConfigurationService;
    private readonly IAppLogger _logger;
    private readonly RelayCommand _saveSettingsCommand;
    private readonly RelayCommand _restoreDefaultsCommand;
    private readonly RelayCommand _openFolderCommand;
    private readonly RelayCommand _openAgentConfigurationCommand;
    private readonly AsyncRelayCommand _validateAgentConfigurationCommand;
    private string _cachePath = string.Empty;
    private string _dataPath = string.Empty;
    private string _logPath = string.Empty;
    private string _statusMessage = "就绪";

    public SettingsViewModel(
        ISettingsService settingsService,
        IAgentConfigurationService agentConfigurationService,
        ChatWorkspaceViewModel chatWorkspaceViewModel,
        IAppLogger logger)
    {
        _settingsService = settingsService;
        _agentConfigurationService = agentConfigurationService;
        _logger = logger;
        _saveSettingsCommand = new RelayCommand(SaveSettings, CanSaveSettings);
        _restoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        _openFolderCommand = new RelayCommand(OpenFolder, CanOpenFolder);
        _openAgentConfigurationCommand = new RelayCommand(OpenAgentConfiguration);
        _validateAgentConfigurationCommand = new AsyncRelayCommand(ValidateAgentConfigurationAsync);
        Reload();
    }

    public event EventHandler? SaveCompleted;

    public string CachePath
    {
        get => _cachePath;
        set
        {
            if (SetProperty(ref _cachePath, value))
            {
                NotifyCommandStates();
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
                NotifyCommandStates();
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
                NotifyCommandStates();
            }
        }
    }

    public string AgentConfigurationPath => _agentConfigurationService.ConfigurationFilePath;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand SaveSettingsCommand => _saveSettingsCommand;

    public RelayCommand RestoreDefaultsCommand => _restoreDefaultsCommand;

    public RelayCommand OpenFolderCommand => _openFolderCommand;

    public RelayCommand OpenAgentConfigurationCommand => _openAgentConfigurationCommand;

    public AsyncRelayCommand ValidateAgentConfigurationCommand => _validateAgentConfigurationCommand;

    public void Reload()
    {
        var settings = _settingsService.CurrentSettings;
        CachePath = settings.CachePath;
        DataPath = settings.DataPath;
        LogPath = settings.LogPath;
        OnPropertyChanged(nameof(AgentConfigurationPath));
        StatusMessage = "已加载当前设置";
    }

    private void SaveSettings()
    {
        var settings = _settingsService.CurrentSettings with
        {
            CachePath = CachePath.Trim(),
            DataPath = DataPath.Trim(),
            LogPath = LogPath.Trim(),
        };

        _settingsService.Save(settings);
        StatusMessage = "目录设置已保存，重启后聊天日志路径生效";
        _logger.Log("保存目录设置");
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSaveSettings()
    {
        return !string.IsNullOrWhiteSpace(CachePath) &&
               !string.IsNullOrWhiteSpace(DataPath) &&
               !string.IsNullOrWhiteSpace(LogPath);
    }

    private void RestoreDefaults()
    {
        _settingsService.RestoreDefaults();
        Reload();
        StatusMessage = "已恢复默认目录设置";
        _logger.Log("恢复默认目录设置");
    }

    private void OpenAgentConfiguration()
    {
        _agentConfigurationService.EnsureTemplateExists();
        Process.Start(new ProcessStartInfo
        {
            FileName = AgentConfigurationPath,
            UseShellExecute = true,
        });
        StatusMessage = "已打开 Agent 配置文件";
    }

    private async Task ValidateAgentConfigurationAsync()
    {
        try
        {
            var result = await _agentConfigurationService.LoadAsync();
            StatusMessage = result.IsDebugFallback
                ? "正式配置不可用，Debug 校验已回退到本地配置"
                : "Agent 配置校验通过，重启应用后生效";
            _logger.Log($"Agent 配置校验通过，来源：{result.SourceDescription}");
        }
        catch (Exception exception)
        {
            StatusMessage = $"Agent 配置校验失败：{exception.Message}";
            _logger.Log($"Agent 配置校验失败：{exception}");
        }
    }

    private void OpenFolder(object? parameter)
    {
        if (parameter is not string path || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalizedPath = path.Trim();
        Directory.CreateDirectory(normalizedPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = normalizedPath,
            UseShellExecute = true,
        });

        _logger.Log($"打开目录：{normalizedPath}");
    }

    private bool CanOpenFolder(object? parameter)
    {
        return parameter is string path && !string.IsNullOrWhiteSpace(path);
    }

    private void NotifyCommandStates()
    {
        _saveSettingsCommand.NotifyCanExecuteChanged();
        _openFolderCommand.NotifyCanExecuteChanged();
    }
}
