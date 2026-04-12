using DeepSeekWpf.Infrastructure;
using DeepSeekWpf.Models;
using DeepSeekWpf.Services;

namespace DeepSeekWpf.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ChatWorkspaceViewModel _chatWorkspaceViewModel;
    private readonly IAppLogger _logger;
    private readonly RelayCommand _saveSettingsCommand;
    private readonly RelayCommand _restoreDefaultsCommand;
    private string _cachePath = string.Empty;
    private string _logPath = string.Empty;
    private string _modelName = string.Empty;
    private string _statusMessage = "就绪";

    public SettingsViewModel(
        ISettingsService settingsService,
        ChatWorkspaceViewModel chatWorkspaceViewModel,
        IAppLogger logger)
    {
        _settingsService = settingsService;
        _chatWorkspaceViewModel = chatWorkspaceViewModel;
        _logger = logger;
        _saveSettingsCommand = new RelayCommand(SaveSettings, CanSaveSettings);
        _restoreDefaultsCommand = new RelayCommand(RestoreDefaults);

        Reload();
    }

    public string CachePath
    {
        get => _cachePath;
        set
        {
            if (SetProperty(ref _cachePath, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
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
            }
        }
    }

    public string ModelName
    {
        get => _modelName;
        set
        {
            if (SetProperty(ref _modelName, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand SaveSettingsCommand => _saveSettingsCommand;

    public RelayCommand RestoreDefaultsCommand => _restoreDefaultsCommand;

    public void Reload()
    {
        var settings = _settingsService.CurrentSettings;
        CachePath = settings.CachePath;
        LogPath = settings.LogPath;
        ModelName = settings.ModelName;
        StatusMessage = "已加载当前设置";
    }

    private void SaveSettings()
    {
        var settings = new AppSettings
        {
            CachePath = CachePath.Trim(),
            LogPath = LogPath.Trim(),
            ModelName = ModelName.Trim(),
        };

        _settingsService.Save(settings);
        _chatWorkspaceViewModel.ReloadSessions();
        StatusMessage = "设置已保存";
        _logger.Log("保存设置");
    }

    private bool CanSaveSettings()
    {
        return !string.IsNullOrWhiteSpace(CachePath) &&
               !string.IsNullOrWhiteSpace(LogPath) &&
               !string.IsNullOrWhiteSpace(ModelName);
    }

    private void RestoreDefaults()
    {
        _settingsService.RestoreDefaults();
        Reload();
        _chatWorkspaceViewModel.ReloadSessions();
        StatusMessage = "已恢复默认设置";
        _logger.Log("恢复默认设置");
    }
}
