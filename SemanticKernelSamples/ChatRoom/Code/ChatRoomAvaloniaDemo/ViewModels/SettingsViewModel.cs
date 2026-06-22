using AgentLib.Model;

using ChatRoomAvaloniaDemo.Models;

using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 设置页 ViewModel。管理模型提供商列表和全局配置项的编辑。
/// </summary>
public sealed class SettingsViewModel : NotifyBase
{
    private readonly AppConfig _appConfig;
    private readonly AppConfig _editableCopy;
    private ModelItemConfig? _selectedDefaultModel;
    private bool _hasChanges;

    /// <summary>
    /// 创建设置页 ViewModel。基于指定应用配置创建可编辑副本。
    /// </summary>
    /// <param name="appConfig">应用配置。</param>
    public SettingsViewModel(AppConfig? appConfig)
    {
        _appConfig = appConfig ?? new AppConfig();
        _editableCopy = new AppConfig
        {
            PersistenceBasePath = _appConfig.PersistenceBasePath,
            SkillFoldersBasePath = _appConfig.SkillFoldersBasePath,
            DefaultMaxRounds = _appConfig.DefaultMaxRounds,
            DefaultModelProviderName = _appConfig.DefaultModelProviderName,
            PrimaryModelId = _appConfig.PrimaryModelId,
        };

        foreach (var provider in _appConfig.Providers)
        {
            var providerCopy = new ModelProviderConfig
            {
                ProviderName = provider.ProviderName,
                ApiEndpoint = provider.ApiEndpoint,
                ApiKey = provider.ApiKey,
                PrimaryModelId = provider.PrimaryModelId,
            };

            foreach (var model in provider.Models)
            {
                providerCopy.Models.Add(new ModelItemConfig
                {
                    ModelName = model.ModelName,
                    ModelId = model.ModelId,
                    Provider = model.Provider,
                    IsFlash = model.IsFlash,
                });
            }

            _editableCopy.Providers.Add(providerCopy);
        }

        AllModels = BuildAllModelsList();
        UpdateSelectedDefaultModel();

        AddProviderCommand = new DelegateCommand(AddProvider);
        RemoveProviderCommand = new DelegateCommand<ModelProviderConfig?>(RemoveProvider);
        NavigateBackCommand = new DelegateCommand(NavigateBack);
    }

    /// <summary>
    /// 所有可用模型的扁平列表，用于全局默认模型选择。
    /// </summary>
    public ObservableCollection<ModelItemConfig> AllModels { get; }

    /// <summary>
    /// 当前选中的全局默认模型。
    /// </summary>
    public ModelItemConfig? SelectedDefaultModel
    {
        get => _selectedDefaultModel;
        set
        {
            if (SetField(ref _selectedDefaultModel, value) && value is not null)
            {
                _editableCopy.PrimaryModelId = value.ModelName;
                // 查找所属提供商
                var provider = _editableCopy.Providers.FirstOrDefault(p =>
                    p.Models.Any(m => string.Equals(m.ModelName, value.ModelName, StringComparison.OrdinalIgnoreCase)));
                _editableCopy.DefaultModelProviderName = provider?.ProviderName ?? string.Empty;
                _hasChanges = true;
            }
        }
    }

    /// <summary>
    /// 持久化根目录路径。
    /// </summary>
    public string PersistenceBasePath
    {
        get => _editableCopy.PersistenceBasePath;
        set => _editableCopy.PersistenceBasePath = value;
    }

    /// <summary>
    /// 技能文件夹基础路径。
    /// </summary>
    public string SkillFoldersBasePath
    {
        get => _editableCopy.SkillFoldersBasePath;
        set => _editableCopy.SkillFoldersBasePath = value;
    }

    /// <summary>
    /// 默认最大对话轮次。
    /// </summary>
    public int DefaultMaxRounds
    {
        get => _editableCopy.DefaultMaxRounds;
        set => _editableCopy.DefaultMaxRounds = value;
    }

    /// <summary>
    /// 模型提供商列表。
    /// </summary>
    public ObservableCollection<ModelProviderConfig> Providers => _editableCopy.Providers;

    /// <summary>添加提供商命令。</summary>
    public DelegateCommand AddProviderCommand { get; }

    /// <summary>删除提供商命令。</summary>
    public DelegateCommand<ModelProviderConfig?> RemoveProviderCommand { get; }

    /// <summary>返回主界面命令。</summary>
    public DelegateCommand NavigateBackCommand { get; }

    /// <summary>
    /// 返回事件。外部订阅者（如 <see cref="MainViewModel"/>）可据此导航回主界面。
    /// </summary>
    public event EventHandler? NavigateBackRequested;

    /// <summary>
    /// 从原始配置重新同步可编辑副本，并重新匹配选中模型。
    /// 用于每次进入设置页时确保 UI 反映最新已保存的配置值。
    /// </summary>
    public void SyncFromConfig()
    {
        _editableCopy.PersistenceBasePath = _appConfig.PersistenceBasePath;
        _editableCopy.SkillFoldersBasePath = _appConfig.SkillFoldersBasePath;
        _editableCopy.DefaultMaxRounds = _appConfig.DefaultMaxRounds;
        _editableCopy.DefaultModelProviderName = _appConfig.DefaultModelProviderName;
        _editableCopy.PrimaryModelId = _appConfig.PrimaryModelId;

        _editableCopy.Providers.Clear();
        foreach (var provider in _appConfig.Providers)
        {
            var providerCopy = new ModelProviderConfig
            {
                ProviderName = provider.ProviderName,
                ApiEndpoint = provider.ApiEndpoint,
                ApiKey = provider.ApiKey,
                PrimaryModelId = provider.PrimaryModelId,
            };

            foreach (var model in provider.Models)
            {
                providerCopy.Models.Add(new ModelItemConfig
                {
                    ModelName = model.ModelName,
                    ModelId = model.ModelId,
                    Provider = model.Provider,
                    IsFlash = model.IsFlash,
                });
            }

            _editableCopy.Providers.Add(providerCopy);
        }

        RefreshAllModels();
        UpdateSelectedDefaultModel();

        _hasChanges = false;
    }

    /// <summary>
    /// 将可编辑副本的更改写回原始配置对象。
    /// </summary>
    public void ApplyToConfig()
    {
        _appConfig.PersistenceBasePath = _editableCopy.PersistenceBasePath;
        _appConfig.SkillFoldersBasePath = _editableCopy.SkillFoldersBasePath;
        _appConfig.DefaultMaxRounds = _editableCopy.DefaultMaxRounds;
        _appConfig.DefaultModelProviderName = _editableCopy.DefaultModelProviderName;
        _appConfig.PrimaryModelId = _editableCopy.PrimaryModelId;

        _appConfig.Providers.Clear();
        foreach (var provider in _editableCopy.Providers)
        {
            _appConfig.Providers.Add(provider);
        }
    }

    private void NavigateBack()
    {
        ApplyToConfig();
        NavigateBackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSelectedDefaultModel()
    {
        _selectedDefaultModel = AllModels.FirstOrDefault(m =>
            string.Equals(m.ModelName, _editableCopy.PrimaryModelId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ModelId, _editableCopy.PrimaryModelId, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(SelectedDefaultModel));
    }

    private ObservableCollection<ModelItemConfig> BuildAllModelsList()
    {
        var list = new ObservableCollection<ModelItemConfig>();
        foreach (var provider in _editableCopy.Providers)
        {
            foreach (var model in provider.Models)
            {
                list.Add(model);
            }
        }
        return list;
    }

    private void AddProvider()
    {
        Providers.Add(new ModelProviderConfig
        {
            ProviderName = "新提供商",
        });
        RefreshAllModels();
    }

    private void RemoveProvider(ModelProviderConfig? provider)
    {
        if (provider is not null)
        {
            Providers.Remove(provider);
            RefreshAllModels();
        }
    }

    private void RefreshAllModels()
    {
        AllModels.Clear();
        foreach (var model in BuildAllModelsList())
        {
            AllModels.Add(model);
        }
    }
}
