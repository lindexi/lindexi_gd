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
            DefaultModelProviderId = _appConfig.DefaultModelProviderId,
            DefaultModelId = _appConfig.DefaultModelId,
        };

        foreach (var provider in _appConfig.Providers)
        {
            var providerCopy = new ModelProviderConfig
            {
                ProviderId = provider.ProviderId,
                ProviderName = provider.ProviderName,
                ApiEndpoint = provider.ApiEndpoint,
                ApiKey = provider.ApiKey,
                DefaultModelId = provider.DefaultModelId,
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

        // 初始化当前选中的默认模型
        _selectedDefaultModel = AllModels.FirstOrDefault(m =>
            string.Equals(m.ModelName, _editableCopy.DefaultModelId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ModelId, _editableCopy.DefaultModelId, StringComparison.OrdinalIgnoreCase));

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
                _editableCopy.DefaultModelId = value.ModelName;
                // 查找所属提供商
                var provider = _editableCopy.Providers.FirstOrDefault(p =>
                    p.Models.Any(m => string.Equals(m.ModelName, value.ModelName, StringComparison.OrdinalIgnoreCase)));
                _editableCopy.DefaultModelProviderId = provider?.ProviderId ?? string.Empty;
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
    /// 将可编辑副本的更改写回原始配置对象。
    /// </summary>
    public void ApplyToConfig()
    {
        _appConfig.PersistenceBasePath = _editableCopy.PersistenceBasePath;
        _appConfig.SkillFoldersBasePath = _editableCopy.SkillFoldersBasePath;
        _appConfig.DefaultMaxRounds = _editableCopy.DefaultMaxRounds;
        _appConfig.DefaultModelProviderId = _editableCopy.DefaultModelProviderId;
        _appConfig.DefaultModelId = _editableCopy.DefaultModelId;

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
            ProviderId = Guid.NewGuid().ToString("N")[..8],
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