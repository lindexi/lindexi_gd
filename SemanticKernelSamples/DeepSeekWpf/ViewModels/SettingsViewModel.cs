using System.Diagnostics;
using System.IO;
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
    private readonly RelayCommand _openFolderCommand;
    private string _cachePath = string.Empty;
    private string _dataPath = string.Empty;
    private string _logPath = string.Empty;
    private string _modelName = string.Empty;
    private string _apiAddress = string.Empty;
    private string _apiKey = string.Empty;
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
        _openFolderCommand = new RelayCommand(OpenFolder, CanOpenFolder);

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

    public string ApiAddress
    {
        get => _apiAddress;
        set
        {
            if (SetProperty(ref _apiAddress, value))
            {
                _saveSettingsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ApiKey
    {
        get => _apiKey;
        set
        {
            if (SetProperty(ref _apiKey, value))
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

    public RelayCommand OpenFolderCommand => _openFolderCommand;

    public void Reload()
    {
        var settings = _settingsService.CurrentSettings;
        CachePath = settings.CachePath;
        DataPath = settings.DataPath;
        LogPath = settings.LogPath;
        ModelName = settings.ModelName;
        ApiAddress = settings.ApiAddress;
        ApiKey = settings.ApiKey;
        StatusMessage = "已加载当前设置";
    }

    private void SaveSettings()
    {
        var settings = _settingsService.CurrentSettings with
        {
            CachePath = CachePath.Trim(),
            DataPath = DataPath.Trim(),
            LogPath = LogPath.Trim(),
            ModelName = ModelName.Trim(),
            ApiAddress = ApiAddress.Trim(),
            ApiKey = ApiKey.Trim(),
        };

        _settingsService.Save(settings);
        _ = _chatWorkspaceViewModel.ReloadSessionsAsync();
        StatusMessage = "设置已保存";
        _logger.Log("保存设置");
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSaveSettings()
    {
        return !string.IsNullOrWhiteSpace(CachePath) &&
               !string.IsNullOrWhiteSpace(DataPath) &&
               !string.IsNullOrWhiteSpace(LogPath) &&
               !string.IsNullOrWhiteSpace(ModelName) &&
               !string.IsNullOrWhiteSpace(ApiAddress);
    }

    private void RestoreDefaults()
    {
        _settingsService.RestoreDefaults();
        Reload();
        _ = _chatWorkspaceViewModel.ReloadSessionsAsync();
        StatusMessage = "已恢复默认设置";
        _logger.Log("恢复默认设置");
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
}
